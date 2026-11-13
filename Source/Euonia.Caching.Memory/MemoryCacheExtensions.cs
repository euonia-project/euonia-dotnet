using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// 适用于 Microsoft.Extensions.Caching.Memory 缓存句柄的配置生成器扩展。
/// </summary>
internal static class MemoryCacheExtensions
{
	/// <summary>
	/// 用于检查给定 <paramref name="cache"/> 实例中是否存在某个键的扩展方法。
	/// </summary>
	/// <param name="cache">缓存实例。</param>
	/// <param name="key">键。</param>
	/// <returns>如果键存在，则为 <c>True</c>。</returns>
	public static bool Contains(this IMemoryCache cache, object key)
	{
		return cache.TryGetValue(key, out _);
	}

	/// <summary>
	/// 在区域键对应的子键集合中登记子键，使 <see cref="RemoveChildren"/> 能按区域批量清理。
	/// </summary>
	/// <param name="cache">缓存实例。</param>
	/// <param name="parentKey">区域键，其值应为子键集合。</param>
	/// <param name="childKey">要登记的子键。</param>
	/// <exception cref="InvalidOperationException">当 <paramref name="parentKey"/> 对应的值不是子键集合时抛出。</exception>
	/// <remarks>区域键不存在时直接返回，不创建集合。</remarks>
	internal static void RegisterChild(this IMemoryCache cache, object parentKey, object childKey)
	{
		if (!cache.TryGetValue(parentKey, out var keys))
		{
			return;
		}

		if (keys is not ConcurrentDictionary<object, bool> keySet)
		{
			throw new InvalidOperationException(Resources.IDS_PARENT_KEY_NOT_VALID);
		}

		keySet.TryAdd(childKey, true);
	}

	/// <summary>
	/// 移除区域键下登记的全部子键，并保留区域键本身。
	/// </summary>
	/// <param name="cache">缓存实例。</param>
	/// <param name="region">区域键。</param>
	/// <remarks>区域键不存在或其值不是子键集合时不做任何处理。</remarks>
	internal static void RemoveChildren(this IMemoryCache cache, object region)
	{
		if (!cache.TryGetValue(region, out var keys))
		{
			return;
		}

		if (keys is not ConcurrentDictionary<object, bool> keySet)
		{
			return;
		}

		foreach (var key in keySet.Keys)
		{
			cache.Remove(key);
		}
	}
}