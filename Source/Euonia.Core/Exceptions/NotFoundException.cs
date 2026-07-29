using System.Net;

namespace System;

/// <summary>
/// 表示因数据未找到而发生的错误。
/// </summary>
[Serializable, HttpStatusCode(HttpStatusCode.NotFound)]
public class NotFoundException : Exception
{
	private const string DEFAULT_MESSAGE = "Not Found";

	/// <summary>
	/// 初始化 <see cref="NotFoundException"/> 类的新实例。
	/// </summary>
	public NotFoundException()
		: base(DEFAULT_MESSAGE)
	{
	}

	/// <summary>
	/// 初始化 <see cref="NotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public NotFoundException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 初始化 <see cref="NotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public NotFoundException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 初始化 <see cref="NotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public NotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}