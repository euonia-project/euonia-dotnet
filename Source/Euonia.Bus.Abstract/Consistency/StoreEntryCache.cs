using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 按存储实例隔离的条目缓存，供 <see cref="IOutboxStore.GetAndCache"/> 与
/// <see cref="IInboxStore.GetAndCache"/> 的默认接口实现使用。
/// </summary>
/// <remarks>
/// 此前的实现把缓存声明为接口上的 <c>static</c> 字段，导致进程内所有存储实例（多数据库、
/// 多租户、同一进程内并行的多个测试）共享同一份缓存，且仅以消息标识符为键，彼此互相污染。
/// 这里改为以存储实例为键的弱引用表：缓存归属于实例，同时不会阻止存储本身被垃圾回收。
/// </remarks>
/// <typeparam name="TStore">存储接口类型。</typeparam>
/// <typeparam name="TEntry">条目类型。</typeparam>
internal static class StoreEntryCache<TStore, TEntry>
	where TStore : class
{
	private static readonly ConditionalWeakTable<TStore, ConcurrentDictionary<string, TEntry>> _caches = new();

	/// <summary>
	/// 获取指定消息标识符对应的缓存条目；缓存未命中时调用 <paramref name="factory"/> 并写入缓存。
	/// </summary>
	/// <param name="store">缓存所属的存储实例。</param>
	/// <param name="key">消息标识符。</param>
	/// <param name="factory">缓存未命中时用于加载条目的委托。</param>
	/// <returns>缓存或新加载的条目；未找到时返回 <c>default</c>（不写入缓存）。</returns>
	public static TEntry GetOrAdd(TStore store, string key, Func<string, TEntry> factory)
	{
		var cache = _caches.GetOrCreateValue(store);
		if (cache.TryGetValue(key, out var cached))
		{
			return cached;
		}

		var entry = factory(key);
		if (entry != null)
		{
			cache[key] = entry;
		}

		return entry;
	}

	/// <summary>
	/// 清空指定存储实例的缓存。
	/// </summary>
	/// <param name="store">要清空缓存的存储实例。</param>
	public static void Clear(TStore store)
	{
		if (_caches.TryGetValue(store, out var cache))
		{
			cache.Clear();
		}
	}
}
