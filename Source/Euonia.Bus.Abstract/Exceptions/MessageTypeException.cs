namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息类型无效时发生的错误。
/// </summary>
public class MessageTypeException : Exception
{
	/// <summary>
	/// 初始化 <see cref="MessageTypeException"/> 类的新实例。
	/// </summary>
	public MessageTypeException()
		: this("The message type is invalid.")
	{
	}

	/// <summary>
	/// 初始化 <see cref="MessageTypeException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的异常消息。</param>
	public MessageTypeException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="MessageTypeException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的异常消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessageTypeException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
