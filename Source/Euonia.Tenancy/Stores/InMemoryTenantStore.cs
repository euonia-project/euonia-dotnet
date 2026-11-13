using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 内存租户存储，适用于测试与示例。
/// </summary>
public sealed class InMemoryTenantStore : ITenantStore
{
	private readonly ConcurrentDictionary<string, ITenantInfo> _byId = new(StringComparer.Ordinal);
	private readonly ConcurrentDictionary<string, ITenantInfo> _byIdentifier = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 使用租户集合初始化 <see cref="InMemoryTenantStore"/> 的新实例。
	/// </summary>
	/// <param name="tenants">初始租户集合。</param>
	public InMemoryTenantStore(IEnumerable<ITenantInfo>? tenants = null)
	{
		if (tenants == null)
		{
			return;
		}

		foreach (var tenant in tenants)
		{
			Add(tenant);
		}
	}

	/// <summary>
	/// 添加或替换租户。
	/// </summary>
	/// <param name="tenant">租户信息。</param>
	public void Add(ITenantInfo tenant)
	{
		ArgumentNullException.ThrowIfNull(tenant);
		TenantIdValidator.Validate(tenant.Id);

		_byId[tenant.Id] = tenant;
		if (!string.IsNullOrWhiteSpace(tenant.Identifier))
		{
			_byIdentifier[tenant.Identifier] = tenant;
		}
	}

	/// <summary>
	/// 移除租户。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	/// <returns>移除成功时为 <see langword="true"/>。</returns>
	public bool Remove(string tenantId)
	{
		if (!_byId.TryRemove(tenantId, out var tenant))
		{
			return false;
		}

		if (!string.IsNullOrWhiteSpace(tenant.Identifier))
		{
			_byIdentifier.TryRemove(tenant.Identifier, out _);
		}

		return true;
	}

	/// <inheritdoc />
	public ValueTask<ITenantInfo?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
	{
		_byId.TryGetValue(tenantId, out var tenant);
		return ValueTask.FromResult(tenant);
	}

	/// <inheritdoc />
	public ValueTask<ITenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
	{
		_byIdentifier.TryGetValue(identifier, out var tenant);
		return ValueTask.FromResult(tenant);
	}

	/// <inheritdoc />
	public ValueTask<IReadOnlyList<ITenantInfo>> ListAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult<IReadOnlyList<ITenantInfo>>(_byId.Values.ToArray());
	}

	/// <inheritdoc />
	public ValueTask<bool> IsActiveAsync(string tenantId, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(_byId.TryGetValue(tenantId, out var tenant) && tenant.Status == TenantStatus.Active);
	}
}
