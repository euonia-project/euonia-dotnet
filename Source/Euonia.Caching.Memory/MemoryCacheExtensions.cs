using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// Extensions for the configuration builder specific to Microsoft.Extensions.Caching.Memory cache handle.
/// </summary>
internal static class MemoryCacheExtensions
{
	/// <summary>
	/// Extension method to check if a key exists in the given <paramref name="cache"/> instance.
	/// </summary>
	/// <param name="cache">The cache instance.</param>
	/// <param name="key">The key.</param>
	/// <returns><c>True</c> if the key exists.</returns>
	public static bool Contains(this IMemoryCache cache, object key)
	{
		return cache.TryGetValue(key, out _);
	}

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