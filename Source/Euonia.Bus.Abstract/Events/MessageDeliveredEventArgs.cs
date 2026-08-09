namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息被投递（Dispatch）时触发的事件参数。
/// </summary>
/// <seealso cref="MessageProcessedEventArgs" />
public class MessageDeliveredEventArgs : MessageProcessedEventArgs
{
	/// <summary>
	/// 初始化 <see cref="MessageDeliveredEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="message">被投递的消息实例。</param>
	/// <param name="context">消息上下文，包含消息的处理状态与结果。</param>
	public MessageDeliveredEventArgs(object message, IMessageContext context)
		: base(message, context, MessageProcessType.Dispatch)
	{
	}
}