namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息被接收时触发的事件参数。
/// </summary>
/// <seealso cref="MessageProcessedEventArgs" />
public class MessageReceivedEventArgs : MessageProcessedEventArgs
{
	/// <summary>
	/// 初始化 <see cref="MessageReceivedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="message">被接收的消息实例。</param>
	/// <param name="context">消息上下文，包含消息的处理状态与结果。</param>
	public MessageReceivedEventArgs(object message, IMessageContext context)
		: base(message, context, MessageProcessType.Receive)
	{
	}
}