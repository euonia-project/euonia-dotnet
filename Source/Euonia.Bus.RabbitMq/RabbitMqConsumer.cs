using System.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// <see cref="IConsumer"/> 的 RabbitMQ 实现。
/// 负责从 RabbitMQ 队列中消费消息并交由处理器上下文进行处理。
/// </summary>
internal class RabbitMqConsumer : RabbitMqRecipient, IConsumer
{
	/// <summary>
	/// 初始化 <see cref="RabbitMqConsumer"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="connection">用于建立 RabbitMQ 连接的持久连接。</param>
	/// <param name="handler">用于处理消息的处理器上下文。</param>
	/// <param name="options">RabbitMQ 消息总线的配置选项。</param>
	public RabbitMqConsumer(IServiceProvider provider, IPersistentConnection connection, IHandlerContext handler, IOptions<RabbitMqBusOptions> options)
		: base(provider, connection, handler, options)
	{
	}

	/// <summary>
	/// 初始化 <see cref="RabbitMqConsumer"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="channelName">要监听的通道名称。</param>
	/// <param name="messageType">要处理的消息类型。</param>
	public RabbitMqConsumer(IServiceProvider provider, string channelName, Type messageType)
		: base(provider, channelName, messageType)
	{
	}

	/// <summary>
	/// 获取此消费者的名称。
	/// </summary>
	public string Name => nameof(RabbitMqConsumer);

	/// <inheritdoc />
	protected override bool AutoAck => Options.AutoAck;

	/// <summary>
	/// 启动消费者，在指定通道上声明队列并开始消费消息。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	internal override async Task StartAsync(CancellationToken cancellationToken = default)
	{
		var queueName = RabbitMqDelivery.ResolveQueueName(Options, ChannelName);

		Channel = await Connection.CreateChannelAsync();

		// 单播队列也必须声明死信路由：处理失败时使用 nack(requeue:false)，
		// 没有 DLX 的消息会被 broker 直接丢弃。
		var dlxArguments = await DeclareDeadLetterAsync(Channel, queueName);
		var queueArguments = RabbitMqDelivery.BuildQueueArguments(Options.MaxPriority, dlxArguments);
		await Channel.QueueDeclareAsync(queueName, true, false, false, queueArguments, cancellationToken: cancellationToken);
		await Channel.BasicQosAsync((uint)Math.Max(0, Options.PrefetchSize), (ushort)Math.Clamp(Options.PrefetchCount, 0, ushort.MaxValue), false, cancellationToken: cancellationToken);

		await Channel.BasicConsumeAsync(queueName, Options.AutoAck, Consumer, cancellationToken: cancellationToken);
	}
}