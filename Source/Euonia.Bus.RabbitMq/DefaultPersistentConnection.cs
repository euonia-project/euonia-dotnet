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
	public async Task<IChannel> CreateChannelAsync()
	{
		while (!IsConnected)
		{
			await TryConnectAsync();
			// 在连接失败时抛出异常，提示当前没有可用的 RabbitMQ 连接来执行此操作
			//throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
		}

		return await _connection.CreateChannelAsync();
	}

	/// <summary>
	/// 处理连接被阻塞的事件，尝试重新连接。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="e">包含连接阻塞信息的事件参数。</param>
	private async Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
	{
		if (IsDisposed)
		{
			return;
		}

		_logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

		await TryConnectAsync();
	}

	/// <summary>
	/// 处理连接回调异常的事件，尝试重新连接。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="e">包含回调异常信息的事件参数。</param>
	private async Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
	{
		if (IsDisposed)
		{
			return;
		}

		_logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

		await TryConnectAsync();
	}

	/// <summary>
	/// 处理连接关闭的事件，尝试重新连接。
	/// </summary>
	/// <param name="sender">事件发送方。</param>
	/// <param name="reason">包含连接关闭原因的事件参数。</param>
	private async Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs reason)
	{
		if (IsDisposed)
		{
			return;
		}

		_logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

		await TryConnectAsync();
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
		}
		catch (IOException exception)
		{
			_logger.LogCritical(exception, "{Message}", exception.Message);
		}
	}
}