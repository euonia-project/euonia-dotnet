using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// <see cref="ISubscriber"/> 的 RabbitMQ 实现。
/// 负责订阅 RabbitMQ 主题（Fanout 交换机）并接收多播消息。
/// </summary>
public class RabbitMqSubscriber : RabbitMqRecipient, ISubscriber
{
	/// <summary>
	/// 日志记录器实例。
	/// </summary>
	private readonly ILogger<RabbitMqSubscriber> _logger;

	/// <summary>
	/// 初始化 <see cref="RabbitMqSubscriber"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="connection">用于建立 RabbitMQ 连接的持久连接。</param>
	/// <param name="handler">用于处理消息的处理器上下文。</param>
	/// <param name="options">RabbitMQ 消息总线的配置选项。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public RabbitMqSubscriber(IServiceProvider provider, IPersistentConnection connection, IHandlerContext handler, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
		: base(provider, connection, handler, options)
	{
		_logger = logger.CreateLogger<RabbitMqSubscriber>();
	}

	/// <summary>
	/// 获取此订阅者的名称。
	/// </summary>
	public string Name => nameof(RabbitMqSubscriber);

	/// <summary>
	/// 指示此订阅者不需要向发送方回复响应。
	/// </summary>
	protected override bool ReplyRequired => false;

	/// <summary>
	/// 启动订阅者，声明 Fanout 交换机和队列，绑定路由键并开始消费消息。
	/// </summary>
	/// <param name="channel">要订阅的通道名称。</param>
	internal override async Task StartAsync(string channel)
	{
		Channel = await Connection.CreateChannelAsync();

		// 为主题订阅者声明 Fanout 交换机和队列。
		// 发布到该交换机的所有消息都会路由到绑定该交换机的所有队列。
		await Channel.ExchangeDeclareAsync(channel, ExchangeType.Fanout);

		// 每个订阅者拥有自己的队列来接收消息，
		// 同一订阅者的所有实例将共享同一个队列。
		var subscriptionId = string.Collapse(Options.SubscriptionId, Assembly.GetEntryAssembly()?.FullName, channel);
		var queueName = await Channel.QueueDeclareAsync($"{channel}@{subscriptionId}", true, false, false)
		                             .ContinueWith(task => task.Result.QueueName);

		await Channel.QueueBindAsync(queueName, channel, Options.RoutingKey ?? "*");
		await Channel.BasicConsumeAsync(string.Empty, Options.AutoAck, Consumer);
	}
}