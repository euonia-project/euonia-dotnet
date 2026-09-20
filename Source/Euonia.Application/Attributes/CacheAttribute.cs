namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法的结果需要缓存。
/// </summary>
/// <remarks>
/// 由 <see cref="CacheInterceptor"/> 处理：方法调用前先按缓存键查询，命中则直接返回缓存结果
/// （不再执行方法体），未命中则执行并把结果写回缓存。缓存实现通过容器中的 <see cref="Nerosoft.Euonia.Caching.ICacheService"/> 解析。
/// <para>仅对返回 <see cref="Task{TResult}"/>、<see cref="System.Threading.Tasks.ValueTask{TResult}"/> 或非 void 同步返回值的方法生效。</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheAttribute : Attribute
{
	/// <summary>
	/// 获取或设置缓存键模板。
	/// </summary>
	/// <remarks>
	/// 支持占位符 {service}（服务类型全名）、{method}（方法名）与按序参数占位符（0、1...）。
	/// 未设置时使用默认格式 {service}.{method}:args。
	/// </remarks>
	public string Key { get; set; }

	/// <summary>
	/// 获取或设置缓存过期秒数；不大于 0 表示不设置 TTL。
	/// </summary>
	public int TimeoutSeconds { get; set; }

	/// <summary>
	/// 获取或设置是否使用 UTC 时间解释绝对过期时间（当前仅对相对 TTL 生效，保留以兼容扩展）。
	/// </summary>
	public bool IsUtc { get; set; } = true;
}