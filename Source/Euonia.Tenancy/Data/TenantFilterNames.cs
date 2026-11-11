namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// EF Core 命名查询过滤器名称约定。
/// </summary>
public static class TenantFilterNames
{
	/// <summary>
	/// 租户过滤器名称。
	/// </summary>
	public const string Tenant = "Tenant";

	/// <summary>
	/// 软删除（墓碑）过滤器名称；由仓储适配层使用。
	/// </summary>
	public const string Tombstone = "Tombstone";
}
