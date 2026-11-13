namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户相关错误代码常量。
/// </summary>
public static class TenantFailureCodes
{
	/// <summary>需要租户但未解析到。</summary>
	public const string Required = "TenantRequired";

	/// <summary>租户标识格式非法。</summary>
	public const string Invalid = "TenantInvalid";

	/// <summary>租户不存在。</summary>
	public const string NotFound = "TenantNotFound";

	/// <summary>租户已停用。</summary>
	public const string Suspended = "TenantSuspended";

	/// <summary>租户已归档。</summary>
	public const string Archived = "TenantArchived";

	/// <summary>用户无权访问租户。</summary>
	public const string Forbidden = "TenantForbidden";

	/// <summary>多个租户候选值冲突。</summary>
	public const string Conflict = "TenantConflict";

	/// <summary>租户存储不可用。</summary>
	public const string StoreUnavailable = "TenantStoreUnavailable";

	/// <summary>跨租户写入被拒绝。</summary>
	public const string CrossTenant = "CrossTenantOperation";
}
