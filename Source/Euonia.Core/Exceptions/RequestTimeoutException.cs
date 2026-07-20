using System.Net;

namespace System;

/// <summary>
/// 表示因请求超时而发生的错误。
/// </summary>
[Serializable, HttpStatusCode(HttpStatusCode.RequestTimeout)]
public class RequestTimeoutException : Exception
{
	private const string DEFAULT_MESSAGE = "Request Timeout";

	/// <summary>
	/// 初始化 <see cref="RequestTimeoutException"/> 类的新实例。
	/// </summary>
	public RequestTimeoutException()
		: base(DEFAULT_MESSAGE)
	{
	}

	/// <summary>
	/// 初始化 <see cref="RequestTimeoutException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public RequestTimeoutException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="RequestTimeoutException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public RequestTimeoutException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 初始化 <see cref="RequestTimeoutException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public RequestTimeoutException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}