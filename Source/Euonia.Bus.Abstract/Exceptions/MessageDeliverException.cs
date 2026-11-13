namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息投递过程中发生的错误。
/// </summary>
public class MessageDeliverException : Exception
{
	/// <summary>
	/// 初始化 <see cref="MessageDeliverException"/> 类的新实例。
	/// </summary>
	public MessageDeliverException()
		: base("Error occurred during message deliver.")
	{
	}

	/// <summary>
	/// 初始化 <see cref="MessageDeliverException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的错误消息。</param>
	public MessageDeliverException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="MessageDeliverException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessageDeliverException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
