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
		return await GetOrAddAsync($"id:{tenantId}", () => _inner.FindByIdAsync(tenantId, cancellationToken)).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async ValueTask<ITenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
	{
		return await GetOrAddAsync($"identifier:{identifier}", () => _inner.FindByIdentifierAsync(identifier, cancellationToken)).ConfigureAwait(false);
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
		_cache.TryRemove($"id:{tenantId}", out _);
	}

	/// <summary>
	/// 清除全部缓存。
	/// </summary>
	public void Clear()
	{
		_cache.Clear();
	}

	private async ValueTask<ITenantInfo?> GetOrAddAsync(string key, Func<ValueTask<ITenantInfo?>> factory)
	{
		var now = DateTimeOffset.UtcNow;
		if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > now)
		{
			return entry.Tenant;
		}

		var tenant = await factory().ConfigureAwait(false);
		var duration = _options.CurrentValue.StoreCacheDuration;
		_cache[key] = new Entry(tenant, now + (tenant == null ? TimeSpan.FromSeconds(Math.Min(5, duration.TotalSeconds)) : duration));
		return tenant;
	}

	private sealed record Entry(ITenantInfo? Tenant, DateTimeOffset ExpiresAt);
}
