namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示信号量锁特性，可应用于方法以确保单个进程内的线程安全执行。
/// </summary>
/// <remarks>
/// 继承自 <see cref="LockAttribute"/>，使用 <see cref="SemaphoreSlim"/> 在单个进程内协调多个线程对共享资源的访问，
/// 通过异步等待（<c>WaitAsync</c>）避免阻塞线程。
/// </remarks>
public sealed class SemaphoreLockAttribute : LockAttribute
{
	/// <summary>
	/// 使用指定的令牌和最大并发访问数初始化 <see cref="SemaphoreLockAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="token">锁令牌。可以包含形如 <c>{parameterName}</c> 或 <c>{parameterName.PropertyName}</c> 的占位符，
	/// 在运行时会被替换为被拦截方法对应的实参值。</param>
	/// <param name="maximumCount">允许的最大并发访问数。必须大于零。</param>
	public SemaphoreLockAttribute(string token, int maximumCount = 1)
		: base(token, maximumCount)
	{
	}
}
