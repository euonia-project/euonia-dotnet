namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法执行时按组失效缓存。
/// </summary>
/// <remarks>
/// 由 <see cref="CacheEvictionInterceptor"/> 处理：按 <see cref="Mode"/> 在方法执行后（<see cref="CacheEvictionMode.After"/>，
/// 默认）或执行前（<see cref="CacheEvictionMode.Before"/>）删除 <see cref="ICacheGroupManager"/> 中登记的 <see cref="Groups"/> 全部缓存键。
/// 典型用于数据变更方法（如更新订单）完成后使相关读取缓存失效（After），或由方法自身写回新缓存值的
/// write-through 场景（Before）。
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheEvictAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="CacheEvictAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="groups">需失效的缓存组名。</param>
	public CacheEvictAttribute(params string[] groups)
	{
		Groups = groups ?? [];
	}

	/// <summary>
	/// 获取需失效的缓存组名。
	/// </summary>
	public string[] Groups { get; }

	/// <summary>
	/// 获取或设置失效相对方法执行的时序；默认 <see cref="CacheEvictionMode.After"/>。
	/// </summary>
	public CacheEvictionMode Mode { get; set; } = CacheEvictionMode.After;
}