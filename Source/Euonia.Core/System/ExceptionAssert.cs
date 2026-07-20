namespace System;

/// <summary>
/// 提供在代码执行中用于断言异常的实用方法。
/// </summary>
public static class ExceptionAssert
{
	/// <summary>
	/// 如果给定条件为 true，则抛出指定类型的异常。
	/// </summary>
	/// <typeparam name="TException">要抛出的异常类型。</typeparam>
	/// <param name="condition">要评估的条件。</param>
	/// <param name="message">异常中包含的消息。</param>
	public static void ThrowIf<TException>(bool condition, string message)
		where TException : Exception
	{
		ThrowIf(condition, () => (TException)Activator.CreateInstance(typeof(TException), message)!);
	}

	/// <summary>
	/// 如果给定条件为 true，则使用工厂方法创建并抛出指定类型的异常。
	/// </summary>
	/// <typeparam name="TException">要抛出的异常类型。</typeparam>
	/// <param name="condition">要评估的条件。</param>
	/// <param name="exceptionFactory">创建异常实例的工厂方法。</param>
	public static void ThrowIf<TException>(bool condition, Func<TException> exceptionFactory)
		where TException : Exception
	{
		if (!condition)
		{
			return;
		}

		var exception = exceptionFactory();
		throw exception;
	}

	/// <summary>
	/// 如果给定条件为 true，则抛出指定类型的异常（假定该异常具有无参构造函数）。
	/// </summary>
	/// <typeparam name="TException">要抛出的异常类型。</typeparam>
	/// <param name="condition">要评估的条件。</param>
	public static void ThrowIf<TException>(bool condition)
		where TException : Exception, new()
	{
		ThrowIf(condition, () => new TException());
	}
}