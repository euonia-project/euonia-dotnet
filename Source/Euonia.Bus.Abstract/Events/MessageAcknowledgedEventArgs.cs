namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息被确认（ACK）时触发的事件参数。
/// </summary>
/// <seealso cref="MessageProcessedEventArgs" />
public class MessageAcknowledgedEventArgs : MessageProcessedEventArgs
{
	/// <summary>
	/// 初始化 <see cref="MessageAcknowledgedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="message">被确认的消息实例。</param>
	/// <param name="context">消息上下文，包含消息的处理状态与结果。</param>
	public MessageAcknowledgedEventArgs(object message, IMessageContext context)
		: base(message, context, MessageProcessType.Receive)
	{
	}
}