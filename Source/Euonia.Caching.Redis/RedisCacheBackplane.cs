using System.Diagnostics;
using Nerosoft.Euonia.Caching.Internal;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// Implementation of the cache backplane using a Redis Pub/Sub channel.
/// <para>
/// Redis Pub/Sub is used to send messages to the redis server on any key change, cache clear, region
/// clear or key remove operation.
/// Every cache manager with the same configuration subscribes to the
/// same channel and can react on those messages to keep other cache handles in sync with the 'master'.
/// </para>
/// </summary>
/// <remarks>
/// The cache manager must have at least one cache handle configured with <see cref="CacheHandleConfiguration.IsBackplaneSource"/> set to <c>true</c>.
/// Usually this is the redis cache handle, if configured. It should be the distributed and bottom most cache handle.
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
    /// Initializes a new instance of the <see cref="RedisCacheBackplane"/> class.
    /// </summary>
    /// <param name="configuration">The cache manager configuration.</param>
    /// <param name="connectionString"></param>
    public RedisCacheBackplane(CacheManagerConfiguration configuration, string connectionString)
        : base(configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        _channelName = new RedisChannel(configuration.BackplaneChannelName ?? "CacheManagerBackplane", RedisChannel.PatternMode.Auto);
        _identifier = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());

        var cfg = RedisConfigurations.GetConfiguration(ConfigurationKey!, connectionString);
        _connection = new RedisConnectionManager(cfg);

        RetryHelper.Retry(Subscribe, configuration.RetryTimeout, configuration.MaxRetries);

        // adding additional timer based send message invoke (shouldn't do anything if there are no messages,
        // but in really rare race conditions, it might happen messages do not get send if SendMEssages only get invoked through "NotifyXyz"
        _timer = new Timer(SendMessages, true, 1000, 1000);
    }

    /// <summary>
    /// Notifies other cache clients about a changed cache key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="action">The cache action.</param>
    public override void NotifyChange(string key, CacheItemChangedEventAction action)
    {
        PublishMessage(BackplaneMessage.ForChanged(_identifier, key, action));
    }

    /// <summary>
    /// Notifies other cache clients about a changed cache key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="region">The region.</param>
    /// <param name="action">The cache action.</param>
    public override void NotifyChange(string key, string region, CacheItemChangedEventAction action)
    {
        PublishMessage(BackplaneMessage.ForChanged(_identifier, key, region, action));
    }

    /// <summary>
    /// Notifies other cache clients about a cache clear.
    /// </summary>
    public override void NotifyClear()
    {
        PublishMessage(BackplaneMessage.ForClear(_identifier));
    }

    /// <summary>
    /// Notifies other cache clients about a cache clear region call.
    /// </summary>
    /// <param name="region">The region.</param>
    public override void NotifyClearRegion(string region)
    {
        PublishMessage(BackplaneMessage.ForClearRegion(_identifier, region));
    }

    /// <summary>
    /// Notifies other cache clients about a removed cache key.
    /// </summary>
    /// <param name="key">The key.</param>
    public override void NotifyRemove(string key)
    {
        PublishMessage(BackplaneMessage.ForRemoved(_identifier, key));
    }

    /// <summary>
    /// Notifies other cache clients about a removed cache key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="region">The region.</param>
    public override void NotifyRemove(string key, string region)
    {
        PublishMessage(BackplaneMessage.ForRemoved(_identifier, key, region));
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="managed">
    /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
    /// only unmanaged resources.
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
                // ignore.
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

                                // clearing up only after successfully sending. Basically retrying...
                                _messages.Clear();

                                // reset log limmiter because we just send stuff
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
                        // no messages for this instance
                        return;
                    }

                    // now deserialize all of them (lazy enumerable)
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
