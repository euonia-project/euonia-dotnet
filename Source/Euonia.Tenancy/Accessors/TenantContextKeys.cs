namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 执行上下文扩展槽中与租户相关的键。
/// </summary>
public static class TenantContextKeys
{
	/// <summary>
	/// 完整租户信息键。
	/// </summary>
	public const string Info = "tenant.info";

	/// <summary>
	/// 租户解析失败原因键。
	/// </summary>
	public const string Failure = "tenant.failure";

	/// <summary>
	/// 租户解析候选值键。
	/// </summary>
	public const string Candidates = "tenant.candidates";
}
