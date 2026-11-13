using System.Security.Claims;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 默认租户访问校验：匿名只允许活跃租户；已认证比较活动租户声明；平台管理员角色放行。
/// </summary>
public sealed class DefaultTenantAccessValidator : ITenantAccessValidator
{
	private readonly IOptionsMonitor<MultiTenancyOptions> _options;

	/// <summary>
	/// 初始化 <see cref="DefaultTenantAccessValidator"/> 的新实例。
	/// </summary>
	/// <param name="options">多租户选项。</param>
	public DefaultTenantAccessValidator(IOptionsMonitor<MultiTenancyOptions> options)
	{
		_options = options;
	}

	/// <inheritdoc />
	public ValueTask<bool> CanAccessAsync(ClaimsPrincipal? user, ITenantInfo tenant, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(tenant);

		if (tenant.Status != TenantStatus.Active)
		{
			return ValueTask.FromResult(false);
		}

		var principal = new UserPrincipal(user);
		if (!principal.IsAuthenticated)
		{
			return ValueTask.FromResult(true);
		}

		if (string.Equals(principal.Tenant, tenant.Id, StringComparison.Ordinal))
		{
			return ValueTask.FromResult(true);
		}

		var roles = _options.CurrentValue.CrossTenantRoles;
		return ValueTask.FromResult(roles.Length > 0 && principal.IsInRoles(roles));
	}
}
