namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法执行成功后按组失效缓存。
/// </summary>
/// <remarks>
/// 由 <see cref="CacheEvictionInterceptor"/> 处理：被拦截的方法执行完成后，删除
/// <see cref="ICacheGroupManager"/> 中登记的 <see cref="Groups"/> 全部缓存键。
/// 典型用于数据变更方法（如更新订单）完成后使相关读取缓存失效。
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheEvictAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="CacheEvictAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="groups">执行完成后需失效的缓存组名。</param>
	public CacheEvictAttribute(params string[] groups)
	{
		Groups = groups ?? [];
	}

	/// <summary>
	/// 获取执行完成后需失效的缓存组名。
	/// </summary>
	public string[] Groups { get; }
}