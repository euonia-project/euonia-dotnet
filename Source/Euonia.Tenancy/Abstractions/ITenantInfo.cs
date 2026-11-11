namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户信息契约。
/// </summary>
public interface ITenantInfo
{
	/// <summary>
	/// 获取不可变、对外的租户标识；进入数据行、缓存键与消息元数据。
	/// </summary>
	string Id { get; }

	/// <summary>
	/// 获取人类可读的短标识（如子域名）；仅用于解析，可变更。
	/// </summary>
	string? Identifier { get; }

	/// <summary>
	/// 获取租户显示名称。
	/// </summary>
	string? Name { get; }

	/// <summary>
	/// 获取租户状态。
	/// </summary>
	TenantStatus Status { get; }

	/// <summary>
	/// 获取租户隔离模式。
	/// </summary>
	TenantIsolation Isolation { get; }

	/// <summary>
	/// 获取 <see cref="TenantIsolation.DatabasePerTenant"/> 模式下的连接串。
	/// </summary>
	string? ConnectionString { get; }

	/// <summary>
	/// 获取 <see cref="TenantIsolation.SharedSchema"/> 模式下的 schema 名称。
	/// </summary>
	string? Schema { get; }
}
