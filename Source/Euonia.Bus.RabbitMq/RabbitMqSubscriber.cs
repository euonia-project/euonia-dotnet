using System.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// <see cref="ISubscriber"/> 的 RabbitMQ 实现。
/// 负责订阅 RabbitMQ 主题（Fanout 交换机）并接收多播消息。
/// </summary>
internal class RabbitMqSubscriber : RabbitMqRecipient, ISubscriber
{
	/// <summary>
	/// 初始化 <see cref="RabbitMqSubscriber"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="connection">用于建立 RabbitMQ 连接的持久连接。</param>
	/// <param name="handler">用于处理消息的处理器上下文。</param>
	/// <param name="options">RabbitMQ 消息总线的配置选项。</param>
	public RabbitMqSubscriber(IServiceProvider provider, IPersistentConnection connection, IHandlerContext handler, IOptions<RabbitMqBusOptions> options)
		: base(provider, connection, handler, options)
	{
	}

	/// <summary>
	/// 初始化 <see cref="RabbitMqSubscriber"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="channelName">要订阅的通道名称。</param>
	/// <param name="messageType">要处理的消息类型。</param>
	public RabbitMqSubscriber(IServiceProvider provider, string channelName, Type messageType)
		: base(provider, channelName, messageType)
	{
	}

	/// <summary>
	/// 获取此订阅者的名称。
	/// </summary>
	public string Name => nameof(RabbitMqSubscriber);

	/// <inheritdoc />
	protected override bool AutoAck => Options.AutoAck;

	/// <summary>
	/// 启动订阅者，声明 Fanout 交换机和队列，绑定路由键并开始消费消息。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	internal override async Task StartAsync(CancellationToken cancellationToken = default)
	{
		Channel = await Connection.CreateChannelAsync();

		// 为主题订阅者声明 Fanout 交换机和队列。
		// 发布到该交换机的所有消息都会路由到绑定该交换机的所有队列。
		await Channel.ExchangeDeclareAsync(ChannelName, ExchangeType.Fanout, cancellationToken: cancellationToken);

		// 每个订阅者拥有自己的队列来接收消息，
		// 同一订阅者的所有实例将共享同一个队列。
		var subscriptionId = string.Collapse(Options.SubscriptionId, Assembly.GetEntryAssembly()?.GetName().Name, ChannelName);
		var queueName = await Channel.QueueDeclareAsync($"{ChannelName}@{subscriptionId}", true, false, false, cancellationToken: cancellationToken)
		                             .ContinueWith(task => task.Result.QueueName);

		await Channel.QueueBindAsync(queueName, ChannelName, Options.RoutingKey ?? "*", cancellationToken: cancellationToken);
		// Consume from the declared queue (use the generated queueName) instead of an empty name.
		await Channel.BasicConsumeAsync(queueName, Options.AutoAck, Consumer, cancellationToken: cancellationToken);
	}
}