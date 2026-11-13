namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户生命周期状态。
/// </summary>
public enum TenantStatus
{
	/// <summary>
	/// 活跃；允许读写。
	/// </summary>
	Active = 0,

	/// <summary>
	/// 暂停；默认拒绝访问，存量消息按策略处理。
	/// </summary>
	Suspended = 1,

	/// <summary>
	/// 归档；只读，默认拒绝写入。
	/// </summary>
	Archived = 2,
}
