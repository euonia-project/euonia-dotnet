using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus.Behaviors;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 发件箱（Outbox）后台调度器，定期扫描发送失败的传输记录并重新投递。
/// </summary>
/// <remarks>
/// 调度器由 <see cref="MessageBus"/> 在构造时创建并启动；依赖 <see cref="System.Threading.Timer"/> 定时轮询
/// <see cref="IOutboxStore.GetFailedMessages"/>，使用互斥锁保证同一时刻只有一个扫描周期在运行。
/// 同一批失败记录彼此独立，采用并行重投递以缩短恢复时间。
/// </remarks>
internal sealed class OutboxDispatcher : IDisposable
{
	private static readonly ConcurrentDictionary<Type, MethodInfo> _redeliverMethods = new();

	private readonly IServiceAccessor _accessor;
	private readonly IOutboxStore _store;
	private readonly OutboxOptions _options;
	private readonly ILogger _logger;
	private Timer _timer;
	private int _running;

	/// <summary>
	/// 初始化 <see cref="OutboxDispatcher"/> 类的新实例。
	/// </summary>
	/// <param name="accessor">用于解析管道与传输器服务的访问器。</param>
	/// <param name="store">发件箱存储。</param>
	/// <param name="options">发件箱配置选项。</param>
	public OutboxDispatcher(IServiceAccessor accessor, IOutboxStore store, OutboxOptions options)
	{
		_accessor = accessor;
		_store = store;
		_options = options ?? new OutboxOptions();
		_logger = accessor.GetService<ILoggerFactory>()?.CreateLogger<OutboxDispatcher>();
	}

	/// <summary>
	/// 启动轮询定时器。存储未注册时不执行任何操作。
	/// </summary>
	public void Start()
	{
		if (_store == null)
		{
			return;
		}

		var interval = _options.PollingInterval > TimeSpan.Zero ? _options.PollingInterval : TimeSpan.FromSeconds(60);
		_timer = new Timer(OnTimerTick, null, interval, interval);
	}

	private void OnTimerTick(object state)
	{
		if (Interlocked.Exchange(ref _running, 1) == 1)
		{
			return;
		}

		_ = Task.Run(RetryAllAsync);
	}

	/// <summary>
	/// 重试所有发送失败的传输记录。
	/// </summary>
	public async Task RetryAllAsync()
	{
		try
		{
			var items = _store?.GetFailedMessages();
			if (items == null || items.Count == 0)
			{
				return;
			}

			var tasks = new List<Task>(items.Count);
			foreach (var item in items)
			{
				var entry = _store.GetAndCache(item.MessageId);
				if (entry == null)
				{
					continue;
				}

				if (!CanRetry(item))
				{
					_logger?.LogWarning("Outbox message {MessageId} on transport {Transport} has exceeded the maximum retry attempts and will be skipped.", item.MessageId, item.Name);
					continue;
				}

				tasks.Add(RedeliverSafeAsync(entry.Content, item));
			}

			if (tasks.Count > 0)
			{
				await Task.WhenAll(tasks);
			}
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Outbox retry failed: {Error}", exception.Message);
		}
		finally
		{
			_store?.ClearCache();
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private async Task RedeliverSafeAsync(IMessageEnvelope envelope, OutboxTransport item)
	{
		try
		{
			await RedeliverAsync(envelope, item.Name);
		}
		catch (Exception exception)
		{
			_logger?.LogWarning(exception, "Failed to redeliver outbox message {MessageId} on transport {Transport}.", item.MessageId, item.Name);
		}
	}

	private bool CanRetry(OutboxTransport item)
	{
		return _options.MaxRetryAttempts <= 0 || item.RetryAttempts <= _options.MaxRetryAttempts;
	}

	private Task RedeliverAsync(IMessageEnvelope envelope, string transportName)
	{
		var payloadType = envelope.Payload?.GetType() ?? typeof(object);
		var method = _redeliverMethods.GetOrAdd(payloadType, type => typeof(OutboxDispatcher).GetMethod(nameof(RedeliverCoreAsync), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(type));
		return (Task)method.Invoke(this, [envelope, transportName])!;
	}

	private async Task RedeliverCoreAsync<TMessage>(IMessageEnvelope envelope, string transportName)
	{
		var pipeline = _accessor.GetRequiredService<IPipeline<IMessageEnvelope<TMessage>, Unit>>();
		pipeline.Use(typeof(OutgoingLoggingBehavior<TMessage, Unit>), transportName, _logger);
		pipeline.Use(typeof(OutgoingOutboxBehavior<TMessage, Unit>), transportName);
		pipeline.UseOf(envelope.Payload.GetType(), true);

		var transport = _accessor.GetKeyedService<ITransporter>(transportName) ?? throw new MessageTransportException($"The transport '{transportName}' is not registered.");
		await pipeline.RunAsync((IMessageEnvelope<TMessage>)envelope, message => transport.PublishAsync(message, CancellationToken.None).ContinueWith(_ => Unit.Value, CancellationToken.None));
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_timer?.Dispose();
		_timer = null;
	}
}