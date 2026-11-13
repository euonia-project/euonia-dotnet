using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 从子域名解析租户短标识。
/// </summary>
internal sealed class HostTenantResolver : ITenantResolveContributor
{
	private readonly IOptions<MultiTenancyOptions> _options;

	public HostTenantResolver(IOptions<MultiTenancyOptions> options)
	{
		_options = options;
	}

	public int Order => 300;

	public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		var format = _options.Value.HostFormat;
		if (string.IsNullOrWhiteSpace(format) || string.IsNullOrWhiteSpace(context.Host))
		{
			return ValueTask.FromResult<string?>(null);
		}

		return ValueTask.FromResult(TenantHostParser.Parse(context.Host!, format));
	}
}
