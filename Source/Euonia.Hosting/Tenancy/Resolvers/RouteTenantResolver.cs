using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 从路由参数解析租户短标识。
/// </summary>
internal sealed class RouteTenantResolver : ITenantResolveContributor
{
	public int Order => 400;

	public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(string.IsNullOrWhiteSpace(context.RouteTenant) ? null : context.RouteTenant);
	}
}
