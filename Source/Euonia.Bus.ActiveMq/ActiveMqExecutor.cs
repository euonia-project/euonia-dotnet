namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 表示 ActiveMQ 执行器，用于从指定的消息通道接收并处理消息。
/// </summary>
internal class ActiveMqExecutor : ActiveMqRecipient
{
	/// <summary>
	/// 初始化一个新的 ActiveMqExecutor 实例。
	/// </summary>
	/// <param name="provider">服务提供者，用于解析依赖项。</param>
	/// <param name="channelName">消息通道的名称。</param>
	/// <param name="messageType">消息的类型。</param>
	public ActiveMqExecutor(IServiceProvider provider, string channelName, Type messageType)
		: base(provider, channelName, messageType)
	{
	}

	/// <summary>
	/// 启动 ActiveMQ 执行器，开始接收并处理消息。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	internal override async Task StartAsync(CancellationToken cancellationToken = default)
	{
		Session = await Connection.CreateSessionAsync();
		var destination = await Session.GetQueueAsync(ChannelName);
		Consumer = await Session.CreateConsumerAsync(destination);
		Consumer.AsyncListener += HandleMessageReceivedAsync;
	}
}