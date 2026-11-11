using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 从 <see cref="MultiTenancyOptions.Tenants"/> 读取租户的存储实现，适用于单机/小规模部署。
/// </summary>
public sealed class ConfigurationTenantStore : ITenantStore
{
	private readonly IOptionsMonitor<MultiTenancyOptions> _options;

	/// <summary>
	/// 初始化 <see cref="ConfigurationTenantStore"/> 的新实例。
	/// </summary>
	/// <param name="options">多租户选项。</param>
	public ConfigurationTenantStore(IOptionsMonitor<MultiTenancyOptions> options)
	{
		_options = options;
	}

	/// <inheritdoc />
	public ValueTask<ITenantInfo?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
	{
		var tenant = _options.CurrentValue.Tenants
		                   .FirstOrDefault(x => string.Equals(x.Id, tenantId, StringComparison.Ordinal));
		return ValueTask.FromResult<ITenantInfo?>(tenant);
	}

	/// <inheritdoc />
	public ValueTask<ITenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
	{
		var tenant = _options.CurrentValue.Tenants
		                   .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Identifier)
		                                        && string.Equals(x.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
		return ValueTask.FromResult<ITenantInfo?>(tenant);
	}

	/// <inheritdoc />
	public ValueTask<IReadOnlyList<ITenantInfo>> ListAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult<IReadOnlyList<ITenantInfo>>(_options.CurrentValue.Tenants.ToArray());
	}

	/// <inheritdoc />
	public ValueTask<bool> IsActiveAsync(string tenantId, CancellationToken cancellationToken = default)
	{
		var tenant = _options.CurrentValue.Tenants
		                   .FirstOrDefault(x => string.Equals(x.Id, tenantId, StringComparison.Ordinal));
		return ValueTask.FromResult(tenant?.Status == TenantStatus.Active);
	}
}
