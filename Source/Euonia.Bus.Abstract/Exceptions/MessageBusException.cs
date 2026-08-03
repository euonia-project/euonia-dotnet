namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示由消息总线抛出的异常。
/// </summary>
/// <seealso cref="Exception" />
[Serializable]
public class MessageBusException : Exception
{
	/// <summary>
	/// 与异常关联的消息上下文。
	/// </summary>
	private readonly object _message;

	/// <summary>
	/// 初始化 <see cref="MessageBusException"/> 类的新实例。
	/// </summary>
	/// <param name="messageContext">与异常关联的消息上下文。</param>
	public MessageBusException(object messageContext)
	{
		_message = messageContext;
	}

	/// <summary>
	/// 初始化 <see cref="MessageBusException"/> 类的新实例。
	/// </summary>
	/// <param name="messageContext">与异常关联的消息上下文。</param>
	/// <param name="message">异常消息。</param>
	public MessageBusException(object messageContext, string message)
		: base(message)
	{
		_message = messageContext;
	}

	/// <summary>
	/// 初始化 <see cref="MessageBusException"/> 类的新实例。
	/// </summary>
	/// <param name="messageContext">与异常关联的消息上下文。</param>
	/// <param name="message">异常消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessageBusException(object messageContext, string message, Exception innerException)
		: base(message, innerException)
	{
		_message = messageContext;
	}

	/// <summary>
	/// 获取与异常关联的消息上下文。
	/// </summary>
	/// <value>与异常关联的消息上下文。</value>
	public virtual object MessageContext => _message;
}