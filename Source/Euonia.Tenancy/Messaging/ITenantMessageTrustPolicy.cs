namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义“消息中的租户元数据是否可信”的判定契约。
/// </summary>
public interface ITenantMessageTrustPolicy
{
	/// <summary>
	/// 判断消息来源是否可信。
	/// </summary>
	/// <param name="transport">消息来源传输名称。</param>
	/// <param name="tenantId">消息携带的租户标识。</param>
	/// <returns>可信时为 <see langword="true"/>。</returns>
	bool IsTrusted(string? transport, string? tenantId);
}
