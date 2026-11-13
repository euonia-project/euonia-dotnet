using System.Diagnostics;
using Nerosoft.Euonia.Caching.Internal;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// 基于 Redis 发布/订阅通道的缓存背板实现。
/// <para>
/// 当发生键变更、缓存清空、区域清空或键移除操作时，使用 Redis 发布/订阅向 redis 服务器发送消息。
/// 所有使用相同配置的缓存管理器都会订阅同一通道，并可以响应这些消息，
/// 以保持其他缓存句柄与“主”缓存同步。
/// </para>
/// </summary>
/// <remarks>
/// 缓存管理器必须至少配置一个缓存句柄，并将 <see cref="CacheHandleConfiguration.IsBackplaneSource"/> 设置为 <c>true</c>。
/// 通常这是 redis 缓存句柄（如果已配置）。它应当是分布式且最底层的缓存句柄。
/// </remarks>
public sealed class RedisCacheBackplane : CacheBackplane
{
    private const int HARD_LIMIT = 50000;
    private readonly RedisChannel _channelName;
    private readonly byte[] _identifier;
    private readonly RedisConnectionManager _connection;
    private readonly Timer _timer;
    private readonly HashSet<BackplaneMessage> _messages = new();
    private readonly object _messageLock = new();
    private int _skippedMessages;
    private bool _sending;
    private readonly CancellationTokenSource _source = new();
    private bool _loggedLimitWarningOnce;

    /// <summary>
    /// 初始化 <see cref="RedisCacheBackplane"/> 类的新实例。
    /// </summary>
    /// <param name="configuration">缓存管理器配置。</param>
    /// <param name="connectionString">用于建立 Redis 连接的连接字符串；当该配置键尚无已注册配置时使用。</param>
    public RedisCacheBackplane(CacheManagerConfiguration configuration, string connectionString)
        : base(configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        _channelName = new RedisChannel(configuration.BackplaneChannelName ?? "CacheManagerBackplane", RedisChannel.PatternMode.Auto);
        _identifier = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());

        var cfg = RedisConfigurations.GetConfiguration(ConfigurationKey!, connectionString);
        _connection = new RedisConnectionManager(cfg);

        RetryHelper.Retry(Subscribe, configuration.RetryTimeout, configuration.MaxRetries);

        // 额外添加基于计时器的消息发送调用（若没有消息，则不应有任何操作，
        // 但在极其罕见的竞态条件下，如果 SendMessages 仅通过 "NotifyXyz" 触发，消息可能未被发送
        _timer = new Timer(SendMessages, true, 1000, 1000);
    }

    /// <summary>
    /// 通知其他缓存客户端某个缓存键已变更。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="action">缓存操作。</param>
    public override void NotifyChange(string key, CacheItemChangedEventAction action)
    {
        PublishMessage(BackplaneMessage.ForChanged(_identifier, key, action));
    }

    /// <summary>
    /// 通知其他缓存客户端某个缓存键已变更。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    /// <param name="action">缓存操作。</param>
    public override void NotifyChange(string key, string region, CacheItemChangedEventAction action)
    {
        PublishMessage(BackplaneMessage.ForChanged(_identifier, key, region, action));
    }

    /// <summary>
    /// 通知其他缓存客户端缓存已清空。
    /// </summary>
    public override void NotifyClear()
    {
        PublishMessage(BackplaneMessage.ForClear(_identifier));
    }

    /// <summary>
    /// 通知其他缓存客户端某个缓存区域已清空。
    /// </summary>
    /// <param name="region">区域。</param>
    public override void NotifyClearRegion(string region)
    {
        PublishMessage(BackplaneMessage.ForClearRegion(_identifier, region));
    }

    /// <summary>
    /// 通知其他缓存客户端某个缓存键已被移除。
    /// </summary>
    /// <param name="key">键。</param>
    public override void NotifyRemove(string key)
    {
        PublishMessage(BackplaneMessage.ForRemoved(_identifier, key));
    }

    /// <summary>
    /// 通知其他缓存客户端某个缓存键已被移除。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    public override void NotifyRemove(string key, string region)
    {
        PublishMessage(BackplaneMessage.ForRemoved(_identifier, key, region));
    }

    /// <summary>
    /// 释放非托管资源，并可选地释放托管资源。
    /// </summary>
    /// <param name="managed">
    /// 若要同时释放托管和非托管资源，则为 <c>true</c>；若仅释放非托管资源，则为 <c>false</c>。
    /// </param>
    protected override void Dispose(bool managed)
    {
        if (managed)
        {
            try
            {
                _source.Cancel();
                _connection.Subscriber.Unsubscribe(_channelName);
                _timer.Dispose();
            }
            catch
            {
                // 忽略。
            }
        }

        base.Dispose(managed);
    }

    private void Publish(byte[] message)
    {
        _connection.Subscriber.Publish(_channelName, message);
    }

    private void PublishMessage(BackplaneMessage message)
    {
        lock (_messageLock)
        {
            if (message.Action == BackplaneAction.Clear)
            {
                Interlocked.Exchange(ref _skippedMessages, _messages.Count);
                _messages.Clear();
            }

            if (_messages.Count > HARD_LIMIT)
            {
                if (!_loggedLimitWarningOnce)
                {
                    _loggedLimitWarningOnce = true;
                }
            }
            else if (!_messages.Add(message))
            {
                Interlocked.Increment(ref _skippedMessages);
            }

            SendMessages(null);
        }
    }

    private void SendMessages(object? state)
    {
        if (_sending || _messages == null || _messages.Count == 0)
        {
            return;
        }

        Task.Factory.StartNew(
            async _ =>
            {
                if (_sending || _messages == null || _messages.Count == 0)
                {
                    return;
                }

                _sending = true;
                await Task.Delay(10).ConfigureAwait(false);
                byte[] msgs;
                lock (_messageLock)
                {
                    if (_messages is { Count: > 0 })
                    {
                        msgs = BackplaneMessage.Serialize(_messages.ToArray());

                        try
                        {
                            if (msgs != null)
                            {
                                Publish(msgs);
                                Interlocked.Increment(ref SentChunks);
                                Interlocked.Add(ref MessagesSent, _messages.Count);
                                _skippedMessages = 0;

                                // 仅在成功发送后才清空。本质上就是重试……
                                _messages.Clear();

                                // 重置日志限制器，因为刚刚发送了内容
                                _loggedLimitWarningOnce = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex, "Error occurred sending backplane messages.");
                        }
                    }

                    _sending = false;
                }
            },
            this,
            _source.Token,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default)
            .ConfigureAwait(false);
    }

    private void Subscribe()
    {
        _connection.Subscriber.Subscribe(
            _channelName,
            (_, msg) =>
            {
                try
                {
                    var messageBytes = (byte[]?)msg;
                    var messages = BackplaneMessage.Deserialize(messageBytes!, _identifier);

                    if (!messages.Any())
                    {
                        // 没有发往此实例的消息
                        return;
                    }

                    // 现在反序列化全部消息（惰性可枚举）
                    var fullMessages = messages.ToArray();
                    Interlocked.Add(ref MessagesReceived, fullMessages.Length);

                    foreach (var message in fullMessages)
                    {
                        switch (message.Action)
                        {
                            case BackplaneAction.Clear:
                                TriggerCleared();
                                break;

                            case BackplaneAction.ClearRegion:
                                TriggerClearedRegion(message.Region!);
                                break;

                            case BackplaneAction.Changed:
                                if (string.IsNullOrWhiteSpace(message.Region))
                                {
                                    TriggerChanged(message.Key!, message.ChangeAction);
                                }
                                else
                                {
                                    TriggerChanged(message.Key!, message.Region, message.ChangeAction);
                                }
                                break;

                            case BackplaneAction.Removed:
                                if (string.IsNullOrWhiteSpace(message.Region))
                                {
                                    TriggerRemoved(message.Key!);
                                }
                                else
                                {
                                    TriggerRemoved(message.Key!, message.Region);
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex, "Error reading backplane message(s)");
                }
            },
            CommandFlags.FireAndForget);
    }
}
