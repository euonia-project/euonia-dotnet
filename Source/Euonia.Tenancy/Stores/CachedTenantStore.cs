using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 为租户存储提供 TTL 缓存与负缓存的装饰器。
/// </summary>
public sealed class CachedTenantStore : ITenantStore
{
	private readonly ITenantStore _inner;
	private readonly IOptionsMonitor<MultiTenancyOptions> _options;
	private readonly ConcurrentDictionary<string, Entry> _cache = new(StringComparer.Ordinal);
	private readonly ConcurrentDictionary<string, Lazy<Task<ITenantInfo?>>> _inflight = new(StringComparer.Ordinal);
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _identifiers = new(StringComparer.Ordinal);
	private readonly object _cacheLock = new();
	private long _version;

	/// <summary>
	/// 初始化 <see cref="CachedTenantStore"/> 的新实例。
	/// </summary>
	/// <param name="inner">被装饰的租户存储。</param>
	/// <param name="options">多租户选项。</param>
	public CachedTenantStore(ITenantStore inner, IOptionsMonitor<MultiTenancyOptions> options)
	{
		_inner = inner;
		_options = options;
	}

	/// <inheritdoc />
	public async ValueTask<ITenantInfo?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
	{
		var tenant = await GetOrAddAsync($"id:{tenantId}", token => _inner.FindByIdAsync(tenantId, token), cancellationToken).ConfigureAwait(false);
		if (!string.IsNullOrEmpty(tenant?.Identifier))
		{
			TrackIdentifier(tenant.Id, tenant.Identifier);
		}

		return tenant;
	}

	/// <inheritdoc />
	public async ValueTask<ITenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
	{
		var tenant = await GetOrAddAsync($"identifier:{identifier}", token => _inner.FindByIdentifierAsync(identifier, token), cancellationToken).ConfigureAwait(false);
		if (tenant != null)
		{
			// 记录 id → identifier 反向索引，供 Invalidate 同时清理标识查询缓存。
			TrackIdentifier(tenant.Id, identifier);
		}

		return tenant;
	}

	/// <inheritdoc />
	public ValueTask<IReadOnlyList<ITenantInfo>> ListAsync(CancellationToken cancellationToken = default)
	{
		return _inner.ListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<bool> IsActiveAsync(string tenantId, CancellationToken cancellationToken = default)
	{
		var tenant = await FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
		return tenant?.Status == TenantStatus.Active;
	}

	/// <summary>
	/// 清除指定租户的缓存。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	public void Invalidate(string tenantId)
	{
		lock (_cacheLock)
		{
			// 版本号让在途加载在写回前发现“已失效”，避免旧值复活。
			Interlocked.Increment(ref _version);
			_cache.TryRemove($"id:{tenantId}", out _);
			_inflight.TryRemove($"id:{tenantId}", out _);

			if (_identifiers.TryRemove(tenantId, out var identifiers))
			{
				foreach (var identifier in identifiers.Keys)
				{
					_cache.TryRemove($"identifier:{identifier}", out _);
					_inflight.TryRemove($"identifier:{identifier}", out _);
				}
			}
		}
	}

	/// <summary>
	/// 清除全部缓存。
	/// </summary>
	public void Clear()
	{
		lock (_cacheLock)
		{
			Interlocked.Increment(ref _version);
			_cache.Clear();
			_identifiers.Clear();
			_inflight.Clear();
		}
	}

	private async ValueTask<ITenantInfo?> GetOrAddAsync(string key, Func<CancellationToken, ValueTask<ITenantInfo?>> factory, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;
		if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > now)
		{
			return entry.Tenant;
		}

		// 同 key 并发请求合并为一次加载，避免缓存过期瞬间击穿到存储。
		var version = Volatile.Read(ref _version);
		var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<ITenantInfo?>>(async () =>
		{
			// 共享加载不绑定首个请求的取消令牌，等待者各自取消。
			var loaded = await factory(CancellationToken.None).ConfigureAwait(false);
			lock (_cacheLock)
			{
				// 版本比较与写回在同一临界区，避免 Invalidate 在两者之间完成。
				if (Volatile.Read(ref _version) == version)
				{
					var expiresAt = DateTimeOffset.UtcNow + (loaded == null ? TimeSpan.FromSeconds(Math.Min(5, _options.CurrentValue.StoreCacheDuration.TotalSeconds)) : _options.CurrentValue.StoreCacheDuration);
					_cache[key] = new Entry(loaded, expiresAt);
					PruneIfNeeded(DateTimeOffset.UtcNow);
				}
			}

			return loaded;
		}, LazyThreadSafetyMode.ExecutionAndPublication));

		var task = lazy.Value;
		try
		{
			// 等待者各自的取消令牌只取消自己的等待，不影响共享加载。
			return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			if (task.IsCompleted)
			{
				_inflight.TryRemove(new KeyValuePair<string, Lazy<Task<ITenantInfo?>>>(key, lazy));
			}
			else
			{
				_ = task.ContinueWith(
					(_, state) => ((ConcurrentDictionary<string, Lazy<Task<ITenantInfo?>>>)state!).TryRemove(
						new KeyValuePair<string, Lazy<Task<ITenantInfo?>>>(key, lazy)),
					_inflight,
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
			}
		}
	}

	private void TrackIdentifier(string tenantId, string identifier)
	{
		if (!_identifiers.ContainsKey(tenantId) && _identifiers.Count >= 1024)
		{
			// 反向索引有上界：淘汰一个最旧租户及其标识缓存，保证“缓存存在 ⇒ 可失效”。
			var oldest = _identifiers.Keys.FirstOrDefault();
			if (oldest != null && _identifiers.TryRemove(oldest, out var staleIdentifiers))
			{
				foreach (var staleIdentifier in staleIdentifiers.Keys)
				{
					_cache.TryRemove($"identifier:{staleIdentifier}", out _);
				}
			}
		}

		_identifiers.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))[identifier] = 0;
	}

	private void PruneIfNeeded(DateTimeOffset now)
	{
		if (_cache.Count <= 1024)
		{
			return;
		}

		foreach (var pair in _cache)
		{
			if (pair.Value.ExpiresAt <= now)
			{
				_cache.TryRemove(pair.Key, out _);
			}
		}

		// 全部仍在有效期内时按任意顺序淘汰到上限，避免每次写入全量排序。
		var overflow = _cache.Count - 1024;
		if (overflow > 0)
		{
			foreach (var pair in _cache.Take(overflow))
			{
				_cache.TryRemove(pair.Key, out _);
			}
		}
	}
	private sealed record Entry(ITenantInfo? Tenant, DateTimeOffset ExpiresAt);
}
