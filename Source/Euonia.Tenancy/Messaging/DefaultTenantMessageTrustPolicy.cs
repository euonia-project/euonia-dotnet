using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 默认消息租户信任策略：内部通道可信；外部 Broker 需显式开启并列入白名单。
/// </summary>
public sealed class DefaultTenantMessageTrustPolicy : ITenantMessageTrustPolicy
{
	private readonly IOptionsMonitor<MultiTenancyOptions> _options;

	/// <summary>
	/// 初始化 <see cref="DefaultTenantMessageTrustPolicy"/> 的新实例。
	/// </summary>
	/// <param name="options">多租户选项。</param>
	public DefaultTenantMessageTrustPolicy(IOptionsMonitor<MultiTenancyOptions> options)
	{
		_options = options;
	}

	/// <inheritdoc />
	public bool IsTrusted(string? transport, string? tenantId)
	{
		return TenantMessageTrustPolicy.IsTrusted(_options.CurrentValue, transport, tenantId);
	}
}
