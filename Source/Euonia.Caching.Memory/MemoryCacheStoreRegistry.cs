using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;

namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// 按 <see cref="MemoryCacheOptions"/> 实例共享的 <see cref="MemoryCache"/> 存储注册表。
/// </summary>
/// <remarks>
/// 句柄按 <c>TCacheValue</c> 各建一个，但**键空间不应按值类型分区**：
/// 否则 <c>Remove&lt;object&gt;(key)</c> 这类跨类型操作会落到另一个存储上，变成静默空操作
/// ——框架自身的 <c>CacheGroupManager.Evict</c> 正是这样失效缓存的。
/// <para>
/// 该注册表必须放在**非泛型**类型上：泛型类型上的静态字段对每个封闭泛型各有一份，
/// 若直接写在 <c>MemoryCacheHandle&lt;TCacheValue&gt;</c> 中，每个 <c>TCacheValue</c>
/// 都会得到自己的「共享」注册表，共享也就不存在了。
/// </para>
/// <para>
/// 以 options 实例为键（而非名称字符串），把共享范围限制在「同一份配置」内：
/// 同一容器内复用同一 options 实例的缓存服务共享存储；不同容器（例如并行测试）
/// 各自持有不同的 options 实例，因而互不干扰。
/// </para>
/// </remarks>
internal static class MemoryCacheStoreRegistry
{
	private static readonly ConditionalWeakTable<MemoryCacheOptions, Lazy<MemoryCache>> _stores = new();

	/// <summary>
	/// 获取指定配置对应的共享存储；不存在时创建一次。
	/// </summary>
	/// <param name="options">缓存配置。</param>
	/// <returns>该配置对应的 <see cref="MemoryCache"/> 实例。</returns>
	public static MemoryCache GetOrCreate(MemoryCacheOptions options)
	{
		// 以 Lazy 承载，保证并发下只创建一个实例（ConditionalWeakTable 的工厂不保证仅执行一次）。
		return _stores
		       .GetValue(options, key => new Lazy<MemoryCache>(() => new MemoryCache(key), LazyThreadSafetyMode.ExecutionAndPublication))
		       .Value;
	}
}
