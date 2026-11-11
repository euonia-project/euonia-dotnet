using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 从活动租户声明解析租户；已认证用户的权威来源。
/// </summary>
internal sealed class ClaimTenantResolver : ITenantResolveContributor
{
	private readonly IOptions<MultiTenancyOptions> _options;

	public ClaimTenantResolver(IOptions<MultiTenancyOptions> options)
	{
		_options = options;
	}

	public int Order => 200;

	public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		var value = context.User?.FindFirst(_options.Value.ClaimType)?.Value;
		return ValueTask.FromResult(string.IsNullOrWhiteSpace(value) ? null : value);
	}
}
