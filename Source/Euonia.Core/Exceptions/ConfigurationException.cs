namespace System;

/// <summary>
/// 表示在应用程序配置期间发生的错误。
/// </summary>
[Serializable]
public class ConfigurationException : Exception
{
    /// <summary>
    /// 初始化 <see cref="ConfigurationException"/> 类的新实例。
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="ConfigurationException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的错误消息。</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息和对导致此异常的内部异常的引用初始化 <see cref="ConfigurationException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的错误消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#pragma warning disable SYSLIB0051
	/// <summary>
	/// 使用序列化数据初始化 <see cref="ConfigurationException"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化对象所需的信息。</param>
	/// <param name="context">序列化的上下文。</param>
	public ConfigurationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
#pragma warning restore SYSLIB0051
}