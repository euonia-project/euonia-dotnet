namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息持久化异常，当消息在持久化过程中发生错误时抛出。
/// </summary>
public class MessagePersistentException : Exception
{
	/// <summary>
	/// 初始化 <see cref="MessagePersistentException"/> 类的新实例。
	/// </summary>
	public MessagePersistentException()
	{
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="MessagePersistentException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的消息。</param>
	public MessagePersistentException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 使用指定的错误消息和内部异常初始化 <see cref="MessagePersistentException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessagePersistentException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}