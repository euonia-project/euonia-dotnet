namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 当消息处理完成时触发的事件参数。
/// </summary>
public class MessageHandledEventArgs : EventArgs
{
	/// <summary>
	/// 初始化 <see cref="MessageHandledEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="messageId">已完成处理的消息标识符。</param>
	public MessageHandledEventArgs(string messageId)
	{
		MessageId = messageId;
	}

	/// <summary>
	/// 获取已完成处理的消息标识符。
	/// </summary>
	public string MessageId { get; }

	/// <summary>
	/// 获取处理程序的类型。
	/// </summary>
	public Type HandlerType { get; internal set; }
}