namespace Nerosoft.Euonia.Application;

/// <summary>
/// 缓存失效相对方法执行时序。
/// </summary>
public enum CacheEvictionMode
{
	/// <summary>
	/// 方法执行成功后失效缓存（默认）。
	/// </summary>
	After = 0,

	/// <summary>
	/// 方法执行前先失效缓存（write-through：方法自身负责写回新值，旧值在执行前已清空）。
	/// </summary>
	Before = 1,
}