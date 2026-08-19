namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示锁特性的基类，可应用于方法以确保安全、互斥的执行。
/// </summary>
/// <remarks>
/// 派生特性定义了锁的作用域：
/// <see cref="SemaphoreLockAttribute"/> 用于单个进程内的线程级锁定；
/// <see cref="DistributedLockAttribute"/> 用于跨进程或跨系统的锁定。
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public abstract class LockAttribute : Attribute
{
	/// <summary>
	/// 使用指定的令牌和最大并发访问数初始化 <see cref="LockAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="token">锁令牌。可以包含形如 <c>{parameterName}</c> 或 <c>{parameterName.PropertyName}</c> 的占位符，
	/// 在运行时会被替换为被拦截方法对应的实参值。</param>
	/// <param name="maximumCount">允许的最大并发访问数。必须大于零。</param>
	/// <exception cref="InvalidOperationException">当 <paramref name="maximumCount"/> 小于或等于零时抛出。</exception>
	protected LockAttribute(string token, int maximumCount = 1)
	{
		Token = token;
		Check.Ensure(maximumCount > 0, nameof(maximumCount), "Maximum count must be greater than zero.");
		MaximumCount = maximumCount;
	}

	/// <summary>
	/// 获取锁令牌。令牌可以包含形如 <c>{parameterName}</c> 或 <c>{parameterName.PropertyName}</c> 的占位符，
	/// 在运行时会被替换为被拦截方法对应的实参值。
	/// </summary>
	public string Token { get; }

	/// <summary>
	/// 获取或设置锁超时时间（毫秒）。默认值为 30000（30 秒）。
	/// </summary>
	/// <exception cref="InvalidOperationException">当赋值为小于或等于零的值时抛出。</exception>
	public int Timeout
	{
		get;
		init
		{
			Check.Ensure(value > 0, nameof(Timeout), "Timeout must be greater than zero.");
			field = value;
		}
	} = 30000;

	/// <summary>
	/// 获取允许的最大并发访问数。默认值为 1。
	/// </summary>
	public int MaximumCount { get; }
}
