namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息传输异常，当消息在传输过程中发生错误时抛出。
/// </summary>
public class MessageTransportException : Exception
{
	/// <summary>
	/// 初始化 <see cref="MessageTransportException"/> 类的新实例。
	/// </summary>
	public MessageTransportException()
	{
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="MessageTransportException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的消息。</param>
	public MessageTransportException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 使用指定的错误消息和内部异常初始化 <see cref="MessageTransportException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessageTransportException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}