namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户数据隔离模式。
/// </summary>
public enum TenantIsolation
{
	/// <summary>
	/// 共享表，行内租户列。
	/// </summary>
	SharedTable = 0,

	/// <summary>
	/// 共享数据库，每租户独立 schema。
	/// </summary>
	SharedSchema = 1,

	/// <summary>
	/// 每租户独立数据库。
	/// </summary>
	DatabasePerTenant = 2,
}
