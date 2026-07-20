using System.Net;

namespace System;

/// <summary>
/// 表示因网关超时而发生的错误。
/// </summary>
[Serializable, HttpStatusCode(HttpStatusCode.GatewayTimeout)]
public class GatewayTimeoutException : Exception
{
	private const string DEFAULT_MESSAGE = "Gateway Timeout";

	/// <summary>
	/// 初始化 <see cref="GatewayTimeoutException"/> 类的新实例。
	/// </summary>
	public GatewayTimeoutException()
		: base(DEFAULT_MESSAGE)
	{
	}

	/// <summary>
	/// 初始化 <see cref="GatewayTimeoutException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public GatewayTimeoutException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="GatewayTimeoutException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public GatewayTimeoutException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 初始化 <see cref="GatewayTimeoutException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public GatewayTimeoutException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}