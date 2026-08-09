namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息约定异常，当消息类型不符合预期的约定（单播/多播/请求）时抛出。
/// </summary>
public class MessageConventionException : Exception
{
	/// <summary>
	/// 初始化 <see cref="MessageConventionException"/> 类的新实例。
	/// </summary>
	public MessageConventionException()
	{
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="MessageConventionException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的消息。</param>
	public MessageConventionException(string message) : base(message)
	{
	}

	/// <summary>
	/// 使用指定的错误消息和内部异常初始化 <see cref="MessageConventionException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的内部异常。</param>
	public MessageConventionException(string message, Exception innerException) : base(message, innerException)
	{
	}
}