namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 表示 ActiveMQ 订阅者，用于订阅指定的消息通道并处理接收到的消息。
/// </summary>
internal class ActiveMqSubscriber : ActiveMqRecipient
{
	/// <summary>
	/// 初始化 <see cref="ActiveMqSubscriber"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析运行时依赖项的服务提供程序。</param>
	/// <param name="channelName">要订阅的消息通道名称。</param>
	/// <param name="messageType">当前订阅者要处理的消息类型。</param>
	public ActiveMqSubscriber(IServiceProvider provider, string channelName, Type messageType)
		: base(provider, channelName, messageType)
	{
	}

	/// <summary>
	/// 启动订阅者并开始监听指定的消息通道。
	/// </summary>
	/// <param name="cancellationToken">用于取消异步启动操作的取消令牌。</param>
	/// <returns>表示异步启动操作的任务。</returns>
	internal override async Task StartAsync(CancellationToken cancellationToken = default)
	{
		Session = await Connection.CreateSessionAsync();

		var destination = await Session.GetTopicAsync(ChannelName);

		Consumer = await Session.CreateSharedConsumerAsync(destination, SubscriptionId);
		Consumer.AsyncListener += HandleMessageReceivedAsync;
	}
}