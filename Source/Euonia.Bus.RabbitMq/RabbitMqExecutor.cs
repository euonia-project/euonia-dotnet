using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// <see cref="IExecutor"/> 的 RabbitMQ 实现。
/// 负责在 RabbitMQ 队列上执行请求-响应（RPC）模式的消息处理。
/// </summary>
public sealed class RabbitMqExecutor : RabbitMqRecipient, IExecutor
{
	/// <summary>
	/// 日志记录器实例。
	/// </summary>
	private readonly ILogger<RabbitMqExecutor> _logger;

	/// <summary>
	/// 初始化 <see cref="RabbitMqExecutor"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="connection">用于建立 RabbitMQ 连接的持久连接。</param>
	/// <param name="handler">用于处理消息的处理器上下文。</param>
	/// <param name="options">RabbitMQ 消息总线的配置选项。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public RabbitMqExecutor(IServiceProvider provider, IPersistentConnection connection, IHandlerContext handler, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
		: base(provider, connection, handler, options)
	{
		_logger = logger.CreateLogger<RabbitMqExecutor>();
	}

	/// <summary>
	/// 获取此执行器的名称。
	/// </summary>
	public string Name => nameof(RabbitMqExecutor);

	/// <summary>
	/// 指示此执行器需要向请求方回复响应。
	/// </summary>
	protected bool ReplyRequired => true;

	/// <summary>
	/// 启动执行器，声明带有死信配置的队列，设置预取策略并开始消费请求消息。
	/// </summary>
	/// <param name="channel">要监听的通道名称。</param>
	internal override async Task StartAsync(string channel)
	{
		var subscriptionId = string.Collapse(Options.SubscriptionId, Assembly.GetEntryAssembly()?.FullName, channel);

		var queueName = $"{channel}@{subscriptionId}";

		Channel = await Connection.CreateChannelAsync();

		var dlxArguments = await DeclareDeadLetterAsync(Channel, queueName);
		await Channel.QueueDeclareAsync(queueName, true, false, false, dlxArguments);
		await Channel.BasicQosAsync((uint)Options.PrefetchSize, (ushort)Options.PrefetchCount, false);

		await Channel.BasicConsumeAsync(queueName, false, Consumer);
	}
}