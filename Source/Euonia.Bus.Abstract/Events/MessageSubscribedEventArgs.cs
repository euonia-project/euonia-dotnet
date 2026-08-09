namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息已被订阅的事件参数。
/// </summary>
public class MessageSubscribedEventArgs : EventArgs
{
	/// <summary>
	/// 初始化 <see cref="MessageSubscribedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="channel">被订阅的通道名称。</param>
	/// <param name="messageType">被订阅的消息类型。</param>
	/// <param name="handlerType">处理该消息的处理程序类型。</param>
	public MessageSubscribedEventArgs(string channel, Type messageType, Type handlerType)
	{
		Channel = channel;
		MessageType = messageType;
		HandlerType = handlerType;
	}

	/// <summary>
	/// 获取被订阅的通道名称。
	/// </summary>
	public string Channel { get; }

	/// <summary>
	/// 获取被订阅的消息类型。
	/// </summary>
	/// <value>被订阅的消息类型。</value>
	public Type MessageType { get; }

	/// <summary>
	/// 获取处理该消息的处理程序类型。
	/// </summary>
	/// <value>处理该消息的处理程序类型。</value>
	public Type HandlerType { get; }
}