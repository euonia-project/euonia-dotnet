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
/// 每个轮询周期与每次重执行都创建独立作用域并从中解析存储，调度器本身不捕获 scoped 服务。
/// </remarks>
internal sealed class InboxDispatcher : IDisposable
{
	private readonly IServiceProvider _provider;
	private readonly IInboxStore _storeOverride;
	private readonly InboxOptions _options;
	private readonly ConcurrentDictionary<string, List<HandlerRegistration>> _container;
	private readonly ILogger _logger;
	private readonly MessageExecutionEnvironment _execution;
	private readonly IServiceProviderIsService _serviceProbe;
	private Timer _timer;
	private int _running;

	/// <summary>
	/// 初始化 <see cref="InboxDispatcher"/> 类的新实例。
	/// </summary>
	/// <param name="provider">宿主容器，用于创建轮询与执行作用域。</param>
	/// <param name="store">收件箱存储覆盖；传 <c>null</c> 时按作用域解析。</param>
	/// <param name="options">收件箱配置选项。</param>
	/// <param name="container">已注册处理程序的容器（与 <see cref="DefaultHandlerContext"/> 共享）。</param>
	public InboxDispatcher(IServiceProvider provider, IInboxStore store, InboxOptions options, ConcurrentDictionary<string, List<HandlerRegistration>> container)
	{
		_provider = provider;
		_storeOverride = store;
		_options = options ?? new InboxOptions();
		_container = container;
		_logger = provider.GetService<ILoggerFactory>()?.CreateLogger<InboxDispatcher>();
		_execution = new MessageExecutionEnvironment(provider, _logger);
		_serviceProbe = provider.GetService<IServiceProviderIsService>();
	}

	/// <summary>
	/// 启动轮询定时器。收件箱未启用或无法确定存储注册时不执行任何操作。
	/// </summary>
	public void Start()
	{
		if (!_options.Enabled)
		{
			return;
		}

		if (_storeOverride == null && _serviceProbe != null && !_serviceProbe.IsService(typeof(IInboxStore)))
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

		BackgroundExecution.Run(RetryAllAsync);
	}

	/// <summary>
	/// 重试所有执行失败的处理记录。
	/// </summary>
	public async Task RetryAllAsync()
	{
		IServiceScope scope = null;
		IInboxStore store = null;

		try
		{
			scope = _provider.CreateScope();
			store = _storeOverride ?? scope.ServiceProvider.GetService<IInboxStore>();
			var deadLetterStore = scope.ServiceProvider.GetService<IDeadLetterStore>();

			// 每轮顺带按保留策略清理已终结的旧条目，避免内存实现无界增长。
			CleanupExpired(store);

			var items = store?.GetFailedMessages();
			if (items == null || items.Count == 0)
			{
				return;
			}

			var tasks = new List<Task>(items.Count);
			foreach (var item in items)
			{
				var entry = store.GetAndCache(item.MessageId);
				if (entry == null)
				{
					continue;
				}

				if (!_container.TryGetValue(entry.Channel, out var registrations) || registrations is not { Count: > 0 })
				{
					_logger?.LogWarning("Inbox message {MessageId} has no handler registered on channel {Channel}; it will be moved to the dead letter store.", item.MessageId, entry.Channel);
					DeadLetter(store, deadLetterStore, entry, item, "No handler is registered on the message channel.");
					continue;
				}

				var registration = registrations.FirstOrDefault(r => string.Equals(r.Name, item.Name, StringComparison.Ordinal));
				if (registration == null)
				{
					_logger?.LogWarning("Inbox message {MessageId} has no handler named {Handler} on channel {Channel}; it will be moved to the dead letter store.", item.MessageId, item.Name, entry.Channel);
					DeadLetter(store, deadLetterStore, entry, item, "The target handler is no longer registered.");
					continue;
				}

				if (!CanRetry(item))
				{
					DeadLetter(store, deadLetterStore, entry, item, item.Error);
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
			Interlocked.Exchange(ref _running, 0);

			try
			{
				store?.ClearCache();
				scope?.Dispose();
			}
			catch (Exception exception)
			{
				_logger?.LogWarning(exception, "Failed to release the inbox retry scope.");
			}
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
	private void CleanupExpired(IInboxStore store)
	{
		if (store == null || _options.RetentionPeriod <= TimeSpan.Zero)
		{
			return;
		}

		try
		{
			store.Cleanup(DateTime.Now - _options.RetentionPeriod);
		}
		catch (Exception exception)
		{
			_logger?.LogWarning(exception, "Failed to clean up expired inbox entries.");
		}
	}

	/// <summary>
	/// 将处理记录转入死信：标记为 <see cref="InboxHandlerStatus.DeadLettered"/>
	/// （从而被 <see cref="IInboxStore.GetFailedMessages"/> 排除，不再被每轮轮询重复扫描），
	/// 并在已注册死信存储时写入一条死信记录。
	/// </summary>
	/// <param name="store">收件箱存储。</param>
	/// <param name="deadLetterStore">死信存储；未注册时为 <c>null</c>。</param>
	/// <param name="entry">消息条目。</param>
	/// <param name="item">处理记录快照。</param>
	/// <param name="error">转入死信的原因；重试耗尽时使用最后一次错误。</param>
	private void DeadLetter(IInboxStore store, IDeadLetterStore deadLetterStore, InboxEntry entry, InboxHandler item, string error)
	{
		// 必须经存储接口持久化终态：item 只是 GetFailedMessages 返回的快照，
		// 直接调用 item.MarkAsDeadLettered 在持久化实现中不会落库，记录会每轮被重复扫描。
		store.MarkAsDeadLettered(entry.MessageId, item.Name, error);
		BusTelemetry.InboxDeadLettered.Add(1, new TagList { { "messaging.channel", entry.Channel } });

		if (deadLetterStore == null)
		{
			_logger?.LogWarning("Inbox message {MessageId} for handler {Handler} was moved to the dead letter state ({Error}) but no IDeadLetterStore is registered. Register one (e.g. services.AddInMemoryDeadLetters()) to capture dead letters.", item.MessageId, item.Name, error);
			return;
		}

		deadLetterStore.Add(new DeadLetterEntry
		{
			MessageId = entry.MessageId,
			Channel = entry.Channel,
			MessageType = entry.MessageType,
			Content = entry.Content,
			Source = DeadLetterSource.Inbox,
			Target = item.Name,
			Error = error,
			RetryAttempts = item.RetryAttempts,
		});

		_logger?.LogWarning("Inbox message {MessageId} for handler {Handler} was moved to the dead letter store ({Error}).", item.MessageId, item.Name, error);
	}

	private async Task ExecuteAsync(HandlerRegistration registration, InboxEntry entry)
	{
		var context = new MessageContext(entry.Content);

		// 重执行按原消息的身份进行，而不是触发本轮重试的调用方身份。
		await _execution.RunAsync(
			MessageExecutionDescriptor.Replay,
			context,
			async provider =>
			{
				var store = _storeOverride ?? provider.GetService<IInboxStore>();
				var handler = registration.Factory(provider);
				try
				{
					await handler(entry.Content.Payload, context, CancellationToken.None);
					store?.MarkAsSuccess(entry.MessageId, registration.Name);
				}
				catch (Exception exception)
				{
					store?.MarkAsFailed(entry.MessageId, registration.Name, exception.Message);
				}
			}).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_timer?.Dispose();
		_timer = null;
	}
}
