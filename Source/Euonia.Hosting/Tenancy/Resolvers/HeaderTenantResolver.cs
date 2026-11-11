using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 从租户请求头解析租户；默认不信任匿名请求。
/// </summary>
internal sealed class HeaderTenantResolver : ITenantResolveContributor
{
	private readonly IOptions<MultiTenancyOptions> _options;

	public HeaderTenantResolver(IOptions<MultiTenancyOptions> options)
	{
		_options = options;
	}

	public int Order => 500;

	public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		var options = _options.Value;
		if (context.Headers == null
		    || !context.Headers.TryGetValue(options.HeaderName, out var value)
		    || string.IsNullOrWhiteSpace(value))
		{
			return ValueTask.FromResult<string?>(null);
		}

		var anonymous = context.User?.Identity?.IsAuthenticated != true;
		if (anonymous && !options.TrustHeaderForAnonymous)
		{
			return ValueTask.FromResult<string?>(null);
		}

		return ValueTask.FromResult<string?>(value);
	}
}
