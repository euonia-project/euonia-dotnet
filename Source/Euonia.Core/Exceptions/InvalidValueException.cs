namespace System;

/// <summary>
/// 表示当值无效时发生的错误。
/// </summary>
[Serializable]
public class InvalidValueException : Exception
{
	/// <summary>
	/// 初始化 <see cref="InvalidValueException"/> 类的新实例。
	/// </summary>
	public InvalidValueException()
	{
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="InvalidValueException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public InvalidValueException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 使用指定的错误消息和对导致此异常的内部异常的引用初始化 <see cref="InvalidValueException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public InvalidValueException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 使用序列化数据初始化 <see cref="InvalidValueException"/> 类的新实例。
	/// </summary>
	/// <param name="info">异常数据的序列化信息。</param>
	/// <param name="context">序列化流的上下文。</param>
	public InvalidValueException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}