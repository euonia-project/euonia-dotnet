namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示熔断器处于打开（或半开不可探测）状态、调用被快速拒绝时抛出的异常。
/// </summary>
public class CircuitBreakerOpenException : Exception
{
	/// <summary>
	/// 初始化 <see cref="CircuitBreakerOpenException"/> 类的新实例。
	/// </summary>
	public CircuitBreakerOpenException()
	{
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="CircuitBreakerOpenException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的详细消息。</param>
	public CircuitBreakerOpenException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// 使用指定的错误消息和内部异常初始化 <see cref="CircuitBreakerOpenException"/> 类的新实例。
	/// </summary>
	/// <param name="message">描述错误的详细消息。</param>
	/// <param name="innerException">导致当前异常的异常。</param>
	public CircuitBreakerOpenException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}