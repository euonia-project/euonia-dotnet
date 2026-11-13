using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 从租户请求头解析租户；默认不信任匿名请求。
/// </summary>
internal sealed class HeaderTenantResolver : ITenantResolveContributor
{
	private readonly IOptionsMonitor<MultiTenancyOptions> _options;

	public HeaderTenantResolver(IOptionsMonitor<MultiTenancyOptions> options)
	{
		_options = options;
	}

	public int Order => 500;

	public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		var options = _options.CurrentValue;
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

		// 未校验的值会进入缓存键与存储查询：超长/含分隔符/控制字符直接视为未解析。
		return TenantIdValidator.IsValidIdentifier(value)
			       ? ValueTask.FromResult<string?>(value)
			       : ValueTask.FromResult<string?>(null);
	}
}
