using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus.Telemetry;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 收件箱（Inbox）后台调度器，定期扫描执行失败的处理记录并重新执行。
/// </summary>
/// <remarks>
/// 调度器由 <see cref="DefaultHandlerContext"/> 在构造时创建并启动；依赖 <see cref="System.Threading.Timer"/> 定时轮询
/// <see cref="IInboxStore.GetFailedMessages"/>，使用互斥锁保证同一时刻只有一个扫描周期在运行。
/// </remarks>
internal sealed class InboxDispatcher : IDisposable
{
	private readonly IServiceProvider _provider;
	private readonly IInboxStore _store;
	private readonly InboxOptions _options;
	private readonly ConcurrentDictionary<string, List<HandlerRegistration>> _container;
	private readonly IDeadLetterStore _deadLetterStore;
	private readonly ILogger _logger;
	private Timer _timer;
	private int _running;

	/// <summary>
	/// 初始化 <see cref="InboxDispatcher"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析处理程序实例的服务提供程序。</param>
	/// <param name="store">收件箱存储。</param>
	/// <param name="options">收件箱配置选项。</param>
	/// <param name="container">已注册处理程序的容器（与 <see cref="DefaultHandlerContext"/> 共享）。</param>
	public InboxDispatcher(IServiceProvider provider, IInboxStore store, InboxOptions options, ConcurrentDictionary<string, List<HandlerRegistration>> container)
	{
		_provider = provider;
		_store = store;
		_options = options ?? new InboxOptions();
		_container = container;
		// 死信存储为可选依赖：未注册时保持原有的"记录警告并跳过"行为。
		_deadLetterStore = provider.GetService<IDeadLetterStore>();
		_logger = provider.GetService<ILoggerFactory>()?.CreateLogger<InboxDispatcher>();
	}

	/// <summary>
	/// 启动轮询定时器。存储未注册或未启用时不执行任何操作。
	/// </summary>
	public void Start()
	{
		if (_store == null || !_options.Enabled)
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
	/// 重试所有执行失败的处理记录。
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

				if (!_container.TryGetValue(entry.Channel, out var registrations) || registrations is not { Count: > 0 })
				{
					_logger?.LogWarning("Inbox message {MessageId} has no handler registered on channel {Channel} and will be skipped.", item.MessageId, entry.Channel);
					continue;
				}

				var registration = registrations.FirstOrDefault(r => string.Equals(r.Name, item.Name, StringComparison.Ordinal));
				if (registration == null)
				{
					_logger?.LogWarning("Inbox message {MessageId} has no handler named {Handler} on channel {Channel} and will be skipped.", item.MessageId, item.Name, entry.Channel);
					continue;
				}

				if (!CanRetry(item))
				{
					DeadLetter(entry, item);
					continue;
				}

				tasks.Add(ExecuteSafeAsync(registration, entry, item));
			}

			if (tasks.Count > 0)
			{
				await Task.WhenAll(tasks);
			}
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Inbox retry failed: {Error}", exception.Message);
		}
		finally
		{
			_store?.ClearCache();
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private async Task ExecuteSafeAsync(HandlerRegistration registration, InboxEntry entry, InboxHandler item)
	{
		BusTelemetry.InboxRetries.Add(1, new TagList { { "messaging.channel", entry.Channel } });

		try
		{
			await ExecuteAsync(registration, entry);
		}
		catch (Exception exception)
		{
			_logger?.LogWarning(exception, "Failed to redeliver inbox message {MessageId} for handler {Handler}.", item.MessageId, item.Name);
		}
	}

	private bool CanRetry(InboxHandler item)
	{
		return _options.MaxRetryAttempts <= 0 || item.RetryAttempts <= _options.MaxRetryAttempts;
	}

	/// <summary>
	/// 按 <see cref="InboxOptions.RetentionPeriod"/> 清理已终结的旧条目。
	/// </summary>
	/// <remarks>
	/// 清理失败不应影响本轮重执行，因此仅记录警告。
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
			_logger?.LogWarning(exception, "Failed to clean up expired inbox entries.");
		}
	}

	/// <summary>
	/// 将重试次数耗尽的处理记录转入死信：标记为 <see cref="InboxHandlerStatus.DeadLettered"/>
	/// （从而被 <see cref="IInboxStore.GetFailedMessages"/> 排除，不再被每轮轮询重复扫描），
	/// 并在已注册死信存储时写入一条死信记录。
	/// </summary>
	private void DeadLetter(InboxEntry entry, InboxHandler item)
	{
		// 必须经存储接口持久化终态：item 只是 GetFailedMessages 返回的快照，
		// 直接调用 item.MarkAsDeadLettered 在持久化实现中不会落库，记录会每轮被重复扫描。
		_store.MarkAsDeadLettered(entry.MessageId, item.Name, item.Error);
		BusTelemetry.InboxDeadLettered.Add(1, new TagList { { "messaging.channel", entry.Channel } });

		if (_deadLetterStore == null)
		{
			_logger?.LogWarning("Inbox message {MessageId} for handler {Handler} has exceeded the maximum retry attempts ({Attempts}) and will be skipped. Register an IDeadLetterStore (e.g. services.AddInMemoryDeadLetters()) to capture dead letters.", item.MessageId, item.Name, item.RetryAttempts);
			return;
		}

		_deadLetterStore.Add(new DeadLetterEntry
		{
			MessageId = entry.MessageId,
			Channel = entry.Channel,
			MessageType = entry.MessageType,
			Content = entry.Content,
			Source = DeadLetterSource.Inbox,
			Target = item.Name,
			Error = item.Error,
			RetryAttempts = item.RetryAttempts,
		});

		_logger?.LogWarning("Inbox message {MessageId} for handler {Handler} has exceeded the maximum retry attempts ({Attempts}) and was moved to the dead letter store.", item.MessageId, item.Name, item.RetryAttempts);
	}

	private async Task ExecuteAsync(HandlerRegistration registration, InboxEntry entry)
	{
		using var scope = _provider.CreateScope();
		var handler = registration.Factory(scope.ServiceProvider);
		var context = new MessageContext(entry.Content);
		try
		{
			await handler(entry.Content.Payload, context, CancellationToken.None);
			_store.MarkAsSuccess(entry.MessageId, registration.Name);
		}
		catch (Exception exception)
		{
			_store.MarkAsFailed(entry.MessageId, registration.Name, exception.Message);
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_timer?.Dispose();
		_timer = null;
	}
}