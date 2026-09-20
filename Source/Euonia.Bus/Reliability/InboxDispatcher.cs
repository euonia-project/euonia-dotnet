using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
			foreach (var item in _store.GetFailedMessages())
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
					_logger?.LogWarning("Inbox message {MessageId} for handler {Handler} has exceeded the maximum retry attempts and will be skipped.", item.MessageId, item.Name);
					continue;
				}

				try
				{
					await ExecuteAsync(registration, entry);
				}
				catch (Exception exception)
				{
					_logger?.LogWarning(exception, "Failed to redeliver inbox message {MessageId} for handler {Handler}.", item.MessageId, item.Name);
				}
			}
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Inbox retry failed: {Error}", exception.Message);
		}
		finally
		{
			_store.ClearCache();
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private bool CanRetry(InboxHandler item)
	{
		return _options.MaxRetryAttempts <= 0 || item.RetryAttempts <= _options.MaxRetryAttempts;
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