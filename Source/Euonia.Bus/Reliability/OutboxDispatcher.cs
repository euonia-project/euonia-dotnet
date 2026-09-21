using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus.Behaviors;
using Nerosoft.Euonia.Bus.Telemetry;
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
	private readonly IDeadLetterStore _deadLetterStore;
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
		// 死信存储为可选依赖：未注册时保持原有的"记录警告并跳过"行为。
		_deadLetterStore = accessor.GetService<IDeadLetterStore>();
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
			// 每轮顺带按保留策略清理已终结的旧条目，避免内存实现无界增长。
			CleanupExpired();

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
					DeadLetter(entry, item);
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
		BusTelemetry.OutboxRetries.Add(1, new TagList { { "messaging.destination.name", item.Name } });

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

	/// <summary>
	/// 按 <see cref="OutboxOptions.RetentionPeriod"/> 清理已终结的旧条目。
	/// </summary>
	/// <remarks>
	/// 清理失败不应影响本轮重投递，因此仅记录警告。
	/// </remarks>
	private void CleanupExpired()
	{
		if (_store == null || _options.RetentionPeriod <= TimeSpan.Zero)
		{
			return;
		}

		try
		{
			_store.Cleanup(DateTime.Now - _options.RetentionPeriod);
		}
		catch (Exception exception)
		{
			_logger?.LogWarning(exception, "Failed to clean up expired outbox entries.");
		}
	}

	/// <summary>
	/// 将重试次数耗尽的传输记录转入死信：标记为 <see cref="OutboxTransportStatus.DeadLettered"/>
	/// （从而被 <see cref="IOutboxStore.GetFailedMessages"/> 排除，不再被每轮轮询重复扫描），
	/// 并在已注册死信存储时写入一条死信记录。
	/// </summary>
	private void DeadLetter(OutboxEntry entry, OutboxTransport item)
	{
		item.MarkAsDeadLettered(item.Error);
		BusTelemetry.OutboxDeadLettered.Add(1, new TagList { { "messaging.channel", entry.Channel }, { "messaging.destination.name", item.Name } });

		if (_deadLetterStore == null)
		{
			_logger?.LogWarning("Outbox message {MessageId} on transport {Transport} has exceeded the maximum retry attempts ({Attempts}) and will be skipped. Register an IDeadLetterStore (e.g. services.AddInMemoryDeadLetters()) to capture dead letters.", item.MessageId, item.Name, item.RetryAttempts);
			return;
		}

		_deadLetterStore.Add(new DeadLetterEntry
		{
			MessageId = entry.MessageId,
			Channel = entry.Channel,
			MessageType = entry.MessageType,
			Content = entry.Content,
			Source = DeadLetterSource.Outbox,
			Target = item.Name,
			Error = item.Error,
			RetryAttempts = item.RetryAttempts,
		});

		_logger?.LogWarning("Outbox message {MessageId} on transport {Transport} has exceeded the maximum retry attempts ({Attempts}) and was moved to the dead letter store.", item.MessageId, item.Name, item.RetryAttempts);
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