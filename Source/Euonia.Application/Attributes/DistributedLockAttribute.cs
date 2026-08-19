namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示分布式锁特性，可应用于方法以确保跨进程或跨系统的安全执行。
/// </summary>
/// <remarks>
/// 继承自 <see cref="LockAttribute"/>，用于在多个进程或系统之间协调对共享资源的访问，
/// 适用于分布式环境下的互斥场景。
/// </remarks>
public sealed class DistributedLockAttribute : LockAttribute
{
	/// <summary>
	/// 使用指定的令牌初始化 <see cref="DistributedLockAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="token">锁令牌。可以包含形如 <c>{parameterName}</c> 或 <c>{parameterName.PropertyName}</c> 的占位符，
	/// 在运行时会被替换为被拦截方法对应的实参值。</param>
	public DistributedLockAttribute(string token)
		: base(token)
	{
	}
}
