using System.Net;

namespace System;

/// <summary>
/// 表示当 HTTP 状态码不是 200 时发生的错误。
/// </summary>
[Serializable]
public class HttpStatusException : Exception
{
	private readonly HttpStatusCode _statusCode;

	/// <summary>
	/// 初始化 <see cref="HttpStatusException"/> 类的新实例。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码。</param>
	public HttpStatusException(HttpStatusCode statusCode)
		: base(statusCode.ToString())
	{
		_statusCode = statusCode;
	}

	/// <summary>
	/// 初始化 <see cref="HttpStatusException"/> 类的新实例。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码。</param>
	/// <param name="message">错误消息。</param>
	public HttpStatusException(HttpStatusCode statusCode, string message)
		: base(message)
	{
		_statusCode = statusCode;
	}

	/// <summary>
	/// 初始化 <see cref="HttpStatusException"/> 类的新实例。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码。</param>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">内部异常。</param>
	public HttpStatusException(HttpStatusCode statusCode, string message, Exception innerException)
		: base(message, innerException)
	{
		_statusCode = statusCode;
	}

	/// <summary>
	/// 获取 HTTP 状态码。
	/// </summary>
	public virtual HttpStatusCode StatusCode => _statusCode;

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 使用序列化数据初始化 <see cref="HttpStatusException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public HttpStatusException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_statusCode = (HttpStatusCode)info.GetInt32(nameof(StatusCode));
	}

#pragma warning disable CS0672
	/// <summary>
	/// 使用将目标对象序列化所需的数据填充 <see cref="SerializationInfo"/>。
	/// </summary>
	/// <param name="info">要填充数据的 <see cref="SerializationInfo"/>。</param>
	/// <param name="context">此序列化的目标。</param>
	public override void GetObjectData(SerializationInfo info, StreamingContext context)

	{
		base.GetObjectData(info, context);
		info.AddValue(nameof(StatusCode), (int)_statusCode, typeof(int));
	}
#pragma warning restore CS0672

#pragma warning restore SYSLIB0051
}