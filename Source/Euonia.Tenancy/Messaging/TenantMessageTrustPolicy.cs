namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 消息租户信任判定的纯函数实现，便于无 DI 场景复用。
/// </summary>
public static class TenantMessageTrustPolicy
{
	private static readonly string[] _internalTransports =
	{
		"inmemory", "outbox", "inbox", "deadletter",
	};

	/// <summary>
	/// 判断消息来源是否可信。
	/// </summary>
	/// <param name="options">多租户选项。</param>
	/// <param name="transport">消息来源传输名称。</param>
	/// <param name="tenantId">消息携带的租户标识。</param>
	/// <returns>可信时为 <see langword="true"/>。</returns>
	public static bool IsTrusted(MultiTenancyOptions options, string? transport, string? tenantId)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (string.IsNullOrWhiteSpace(transport))
		{
			return false;
		}

		if (_internalTransports.Contains(transport, StringComparer.OrdinalIgnoreCase))
		{
			return true;
		}

		return options.TrustBrokerTenant
		       && options.TrustedTransports.Contains(transport, StringComparer.OrdinalIgnoreCase);
	}
}
