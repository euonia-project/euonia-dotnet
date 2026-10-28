using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Threading;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// <see cref="IPersistentConnection"/> 的默认实现。
/// 负责建立、维护和自动重连 RabbitMQ 持久连接，并在连接事件（关闭、异常、阻塞）发生时自动尝试重新连接。
/// </summary>
internal class DefaultPersistentConnection : DisposableObject, IPersistentConnection
{
	/// <summary>
	/// 用于同步连接操作的异步锁。
	/// </summary>
	private readonly AsyncLock _mutex = new();

	/// <summary>
	/// RabbitMQ 连接工厂，用于创建实际的连接实例。
	/// </summary>
	private readonly IConnectionFactory _factory;

	/// <summary>
	/// 日志记录器实例。
	/// </summary>
	private readonly ILogger<DefaultPersistentConnection> _logger;

	/// <summary>
	/// 连接失败时的最大重试次数。
	/// </summary>
	private readonly int _retryCount;

	/// <summary>
	/// 当前的 RabbitMQ 连接实例。
	/// </summary>
	private IConnection _connection;

	/// <summary>
	/// 获取或设置一个值，指示当前实例是否已被释放。
	/// </summary>
	private bool IsDisposed { get; set; }

	/// <summary>
	/// 初始化 <see cref="DefaultPersistentConnection"/> 类的新实例。
	/// </summary>
	/// <param name="factory">用于创建 RabbitMQ 连接的连接工厂。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	/// <param name="options">RabbitMQ 消息总线的配置选项，用于获取最大重试次数。</param>
	public DefaultPersistentConnection(IConnectionFactory factory, ILoggerFactory logger, IOptions<RabbitMqBusOptions> options)
	{
		_factory = factory;
		_logger = logger.CreateLogger<DefaultPersistentConnection>();
		_retryCount = options.Value.MaxFailureRetries;
	}

	/// <summary>
	/// 获取一个值，指示当前 RabbitMQ 连接是否已建立且处于打开状态。
	/// </summary>
	public bool IsConnected => _connection is { IsOpen: true } && !IsDisposed;

	/// <summary>
	/// 尝试建立 RabbitMQ 持久连接。
	/// 若当前已连接则直接返回成功；否则通过 Polly 策略进行带指数退避的自动重试，
	/// 连接成功后订阅连接关闭、回调和阻塞等事件。
	/// </summary>
	/// <returns>连接成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public async Task TryConnectAsync()
	{
		using (await _mutex.LockAsync())
		{
			if (IsConnected)
			{
				return;
			}

			if (_connection != null)
			{
				_connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
				_connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
				_connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
				_connection.Dispose();
				_connection = null;
			}

			_logger.LogInformation("RabbitMQ Client is trying to connect");
			_connection ??= await Policy.Handle<SocketException>()
			                            .Or<BrokerUnreachableException>()
			                            .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
			                            {
				                            _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
			                            })
			                            .ExecuteAsync(() => _factory.CreateConnectionAsync());

			if (IsConnected)
			{
				_connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
				_connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
				_connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;

				_logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);
			}
			else
			{
				_logger.LogCritical("Fatal error: RabbitMQ connections could not be created and opened");
			}
		}
	}

	/// <summary>
	/// 在持久连接上创建一个新的 RabbitMQ 通道。
	/// 若当前未连接，则先尝试建立连接。
	/// </summary>
	/// <returns>表示异步操作的任务，包含创建的 <see cref="IChannel"/> 实例。</returns>
	/// <exception cref="ObjectDisposedException">当连接对象已被释放时抛出。</exception>
	/// <remarks>
	/// 已释放的连接不可能再变为已连接状态：若在此处继续循环等待，
	/// <see cref="TryConnectAsync"/> 会在每轮重新建立并丢弃一个到 broker 的 TCP 连接，
	/// 且循环没有延迟、永不终止。因此这里在已释放时立即失败。
	/// </remarks>
	public Task<IChannel> CreateChannelAsync()
	{
		return CreateChannelAsync(null);
	}

	/// <summary>
	/// 在持久连接上创建一个新的 RabbitMQ 通道，可指定发布确认等选项。
	/// </summary>
	/// <param name="options">通道创建选项；为 <c>null</c> 时使用客户端默认值。</param>
	/// <returns>表示异步操作的任务，包含创建的 <see cref="IChannel"/> 实例。</returns>
	/// <exception cref="ObjectDisposedException">当连接对象已被释放时抛出。</exception>
	public async Task<IChannel> CreateChannelAsync(CreateChannelOptions options)
	{
		while (true)
		{
			ObjectDisposedException.ThrowIf(IsDisposed, this);

			if (_connection is { IsOpen: true })
			{
				return options == null
					       ? await _connection.CreateChannelAsync()
					       : await _connection.CreateChannelAsync(options);
			}

			if (_connection == null)
			{
				await TryConnectAsync();
				continue;
			}

			// 连接对象存在但暂时不可用：交给客户端自动恢复机制。
			// 绝不能在这里销毁并重建连接——那会取消自动恢复，且已启动的消费者不会重新创建。
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
	}

	/// <summary>
	/// 处理连接被阻塞的事件，尝试重新连接。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="e">包含连接阻塞信息的事件参数。</param>
	private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
	{
		if (!IsDisposed)
		{
			// 阻塞是 broker 的流量控制，不是断线；重连反而会中断自动恢复。
			_logger.LogWarning("RabbitMQ connection is blocked: {Reason}", e.Reason);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 处理连接回调异常的事件，尝试重新连接。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="e">包含回调异常信息的事件参数。</param>
	private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
	{
		if (!IsDisposed)
		{
			// 通道/消费者回调异常由客户端的自动恢复处理；此处只负责留下日志。
			_logger.LogWarning(e.Exception, "RabbitMQ callback exception occurred; automatic recovery will handle the channel.");
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 处理连接关闭的事件，尝试重新连接。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="reason">包含连接关闭原因的事件参数。</param>
	private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs reason)
	{
		if (!IsDisposed)
		{
			// 不要销毁并重建连接：客户端会自动恢复连接、通道与消费者；
			// 手动重建会让既有消费者的通道永久失效。
			_logger.LogWarning("RabbitMQ connection was shut down ({ReplyText}); automatic recovery will reconnect.", reason.ReplyText);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 释放连接占用的资源。
	/// 取消订阅连接事件并释放底层连接；捕获并记录释放过程中可能抛出的 <see cref="IOException"/>。
	/// </summary>
	/// <param name="disposing">指示是否正在释放托管资源。</param>
	protected override void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}

		IsDisposed = true;

		try
		{
			if (_connection == null)
			{
				return;
			}

			_connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
			_connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
			_connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
			_connection.Dispose();
			// 必须置空：否则 TryConnectAsync 会看到非空但已释放的连接，
			// 进而每轮新建并丢弃一个 TCP 连接，使 CreateChannelAsync 的等待循环永不终止。
			_connection = null;
		}
		catch (IOException exception)
		{
			_logger.LogCritical(exception, "{Message}", exception.Message);
		}
	}
}