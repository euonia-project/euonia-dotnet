using System.Net.Sockets;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Threading;
using Polly;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// <see cref="IPersistentConnection"/> 的默认实现。
/// </summary>
public class DefaultPersistentConnection : DisposableObject, IPersistentConnection
{
	/// <summary>
	/// 用于同步连接操作的异步锁。
	/// </summary>
	private readonly AsyncLock _mutex = new();

	/// <summary>
	/// ActiveMQ 连接工厂，用于创建实际的连接实例。
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
	/// 初始化 <see cref="DefaultPersistentConnection"/> 类的新实例。
	/// </summary>
	/// <param name="factory">ActiveMQ 连接工厂实例。</param>
	/// <param name="logger">日志记录器工厂实例。</param>
	/// <param name="options">ActiveMQ 总线选项实例。</param>
	public DefaultPersistentConnection(IConnectionFactory factory, ILoggerFactory logger, IOptions<ActiveMqBusOptions> options)
	{
		_factory = factory;
		_logger = logger.CreateLogger<DefaultPersistentConnection>();
		_retryCount = options.Value.MaxFailureRetries;
	}

	/// <summary>
	/// 获取或设置一个值，指示当前实例是否已被释放。
	/// </summary>
	private bool IsDisposed { get; set; }

	/// <summary>
	/// 释放由 <see cref="DefaultPersistentConnection"/> 占用的资源。
	/// </summary>
	/// <param name="disposing"></param>
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

			_connection.ConnectionInterruptedListener -= OnConnectionInterruptedAsync;
			_connection.ExceptionListener -= OnConnectionExceptionAsync;
			_connection.Dispose();
		}
		catch (IOException exception)
		{
			_logger.LogCritical(exception, "{Message}", exception.Message);
		}
	}

	/// <summary>
	/// 获取一个值，指示当前 ActiveMQ 连接是否已建立且处于活动状态。
	/// </summary>
	public bool IsConnected => _connection is { IsStarted: true } && !IsDisposed;

	/// <summary>
	/// 尝试建立 ActiveMQ 持久连接。
	/// </summary>
	public async Task TryConnectAsync()
	{
		using (await _mutex.LockAsync())
		{
			if (IsConnected)
			{
				return;
			}

			_logger.LogInformation("ActiveMQ Client is trying to connect");

			_connection ??= await Policy.Handle<SocketException>()
			                            .Or<Exception>()
			                            .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
			                            {
				                            _logger.LogWarning(ex, "ActiveMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
			                            })
			                            .ExecuteAsync(() => _factory.CreateConnectionAsync());
			await _connection.StartAsync();

			if (IsConnected)
			{
				_connection.ConnectionInterruptedListener += OnConnectionInterruptedAsync;
				_connection.ExceptionListener += OnConnectionExceptionAsync;

				_logger.LogInformation("ActiveMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.ClientId);
			}
			else
			{
				_logger.LogCritical("FATAL ERROR: ActiveMQ connections could not be created and opened");
			}
		}
	}

	/// <summary>
	/// 处理连接异常事件。
	/// </summary>
	/// <param name="exception">发生的异常。</param>
	private void OnConnectionExceptionAsync(Exception exception)
	{
		_logger.LogError(exception, "ActiveMQ connection exception occurred: {Message}", exception.Message);
	}

	/// <summary>
	/// 处理连接中断事件，尝试重新连接。
	/// </summary>
	private void OnConnectionInterruptedAsync()
	{
		if (IsDisposed)
		{
			return;
		}

		AsyncContext.Run(TryConnectAsync);
	}

	/// <summary>
	/// 在持久连接上创建一个新的 ActiveMQ 会话。
	/// </summary>
	/// <returns>表示异步操作的任务，包含创建的 <see cref="ISession"/> 实例。</returns>
	public async Task<ISession> CreateSessionAsync()
	{
		while (!IsConnected)
		{
			await TryConnectAsync();
		}

		return await _connection.CreateSessionAsync();
	}
}