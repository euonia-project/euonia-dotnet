namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息被处理时触发的事件参数基类。
/// </summary>
/// <seealso cref="EventArgs" />
public class MessageProcessedEventArgs : EventArgs
{
	/// <summary>
	/// 初始化 <see cref="MessageProcessedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="message">被处理的消息实例。</param>
	/// <param name="context">消息上下文，包含消息的处理状态与结果。</param>
	/// <param name="processType">消息的处理类型。</param>
	public MessageProcessedEventArgs(object message, IMessageContext context, MessageProcessType processType)
	{
		Message = message;
		Context = context;
		ProcessType = processType;
	}

	/// <summary>
	/// 获取被处理的消息实例。
	/// </summary>
	/// <value>被处理的消息实例。</value>
	public object Message { get; }

	/// <summary>
	/// 获取消息上下文，包含消息的处理状态与结果。
	/// </summary>
	/// <value>消息上下文。</value>
	public IMessageContext Context { get; }

	/// <summary>
	/// 获取消息的处理类型。
	/// </summary>
	/// <value>消息的处理类型。</value>
	public MessageProcessType ProcessType { get; }
}