using System.Collections.Concurrent;
using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// <see cref="ICacheGroupManager"/> 的内存实现，持有缓存键—组索引并调用 <see cref="ICacheService.Remove{TValue}"/> 失效缓存。
/// </summary>
public class CacheGroupManager : ICacheGroupManager
{
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _index = new(StringComparer.Ordinal);
	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	/// 初始化 <see cref="CacheGroupManager"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">用于惰性解析 <see cref="ICacheService"/> 的服务容器。</param>
	public CacheGroupManager(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	/// <inheritdoc />
	public void Register(string key, IEnumerable<string> groups)
	{
		if (string.IsNullOrEmpty(key) || groups == null)
		{
			return;
		}

		foreach (var group in groups)
		{
			if (string.IsNullOrEmpty(group))
			{
				continue;
			}

			var bucket = _index.GetOrAdd(group, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
			bucket[key] = 0;
		}
	}

	/// <inheritdoc />
	public IReadOnlyCollection<string> GetKeys(string group)
	{
		return _index.TryGetValue(group, out var bucket) ? bucket.Keys.ToArray() : [];
	}

	/// <inheritdoc />
	public void Remove(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}

		foreach (var bucket in _index.Values)
		{
			bucket.TryRemove(key, out _);
		}
	}

	/// <inheritdoc />
	public int Evict(IEnumerable<string> groups)
	{
		if (groups == null)
		{
			return 0;
		}

		var cache = _serviceProvider.GetService(typeof(ICacheService)) as ICacheService;
		var removed = 0;
		foreach (var group in groups)
		{
			if (string.IsNullOrEmpty(group) || !_index.TryGetValue(group, out var bucket))
			{
				continue;
			}

			// 先失效条目，再移除索引。
			// 反过来的话，等待"索引被清空"的调用方会在条目仍然存活时就开始读取，
			// 命中本应失效的缓存；中途失败时索引也仍保留这些键，便于重试。
			foreach (var key in bucket.Keys)
			{
				if (cache != null)
				{
					cache.Remove<object>(key);
				}

				removed++;
			}

			_index.TryRemove(group, out _);
		}

		return removed;
	}
}