namespace System;

/// <summary>
/// 表示在业务逻辑执行期间发生的错误。
/// </summary>
[Serializable]
public class BusinessException : Exception
{
	private readonly string _code;

	/// <summary>
	/// 获取业务错误代码。
	/// </summary>
	public virtual string Code => _code;

	/// <summary>
	/// 初始化 <see cref="BusinessException"/> 类的新实例。
	/// </summary>
	public BusinessException()
	{
	}

	/// <summary>
	/// 使用错误代码初始化 <see cref="BusinessException"/> 的新实例。
	/// </summary>
	/// <param name="code">错误代码。</param>
	public BusinessException(string code)
	{
		_code = code;
	}

	/// <summary>
	/// 使用错误代码和消息初始化 <see cref="BusinessException"/> 的新实例。
	/// </summary>
	/// <param name="code">错误代码。</param>
	/// <param name="message">错误消息。</param>
	public BusinessException(string code, string message)
		: base(message)
	{
		_code = code;
	}

	/// <summary>
	/// 使用错误代码、消息和内部异常初始化 <see cref="BusinessException"/> 的新实例。
	/// </summary>
	/// <param name="code">错误代码。</param>
	/// <param name="message">解释异常原因的错误消息。</param>
	/// <param name="innerException">导致当前异常的异常。</param>
	public BusinessException(string code, string message, Exception innerException)
		: base(message, innerException)
	{
		_code = code;
	}

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 使用序列化数据初始化 <see cref="BusinessException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public BusinessException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_code = info.GetString(nameof(Code));
	}

#pragma warning disable CS0672 // Member overrides obsolete member
	/// <summary>
	/// 使用将目标对象序列化所需的数据填充 <see cref="SerializationInfo"/>。
	/// </summary>
	/// <param name="info">要填充数据的 <see cref="SerializationInfo"/>。</param>
	/// <param name="context">此序列化的目标。</param>
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue(nameof(Code), _code, typeof(string));
	}
#pragma warning restore CS0672 // Member overrides obsolete member

#pragma warning restore SYSLIB0051
}