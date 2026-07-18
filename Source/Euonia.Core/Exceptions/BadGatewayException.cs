using System.Net;

namespace System;

/// <summary>
/// 表示当作为网关或代理的服务器在尝试完成请求时，从其访问的上游服务器收到无效响应而发生的错误。
/// </summary>
[Serializable, HttpStatusCode(HttpStatusCode.BadGateway)]
public class BadGatewayException : Exception
{
	private const string DEFAULT_MESSAGE = "Bad Gateway";

	/// <summary>
	/// 初始化 <see cref="BadGatewayException"/> 类的新实例。
	/// </summary>
	public BadGatewayException()
		: base(DEFAULT_MESSAGE)
	{
	}

	/// <summary>
	/// 初始化 <see cref="BadGatewayException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public BadGatewayException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="BadGatewayException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public BadGatewayException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 初始化 <see cref="BadGatewayException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public BadGatewayException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}