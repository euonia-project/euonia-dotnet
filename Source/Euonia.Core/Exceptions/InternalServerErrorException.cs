using System.Net;

namespace System;

/// <summary>
/// 表示发生内部服务器错误时抛出的异常。
/// </summary>
[Serializable, HttpStatusCode(HttpStatusCode.InternalServerError)]
public class InternalServerErrorException : Exception
{
	private const string DEFAULT_MESSAGE = "Internal Server Error";

	/// <summary>
	/// 初始化 <see cref="InternalServerErrorException"/> 类的新实例。
	/// </summary>
	public InternalServerErrorException()
		: base(DEFAULT_MESSAGE)
	{
	}

	/// <summary>
	/// 初始化 <see cref="InternalServerErrorException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public InternalServerErrorException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="InternalServerErrorException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public InternalServerErrorException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 初始化 <see cref="InternalServerErrorException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public InternalServerErrorException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}