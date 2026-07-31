namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息处理异常，当消息在处理过程中发生错误时抛出。
/// </summary>
public class MessageProcessingException : Exception
{
	/// <summary>
	/// 初始化 <see cref="MessageProcessingException"/> 类的新实例。
	/// </summary>
	/// <param name="messageId">消息ID。</param>
	public MessageProcessingException(string messageId)
	{
		MessageId = messageId;
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="MessageProcessingException"/> 类的新实例。
	/// </summary>
	/// <param name="messageId">消息ID。</param>
	/// <param name="message">描述错误的消息。</param>
	public MessageProcessingException(string messageId, string message)
		: base(message)
	{
		MessageId = messageId;
	}

	/// <summary>
	/// 使用指定的错误消息和内部异常初始化 <see cref="MessageProcessingException"/> 类的新实例。
	/// </summary>
	/// <param name="messageId">消息ID。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessageProcessingException(string messageId, string message, Exception innerException)
		: base(message, innerException)
	{
		MessageId = messageId;
	}

	/// <summary>
	/// 获取消息Id
	/// </summary>
	public string MessageId { get; }
}