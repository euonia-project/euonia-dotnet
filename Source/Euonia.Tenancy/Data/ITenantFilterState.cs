namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 提供查询过滤器所需的租户状态；由持久化适配层实现。
/// </summary>
public interface ITenantFilterState
{
	/// <summary>
	/// 获取当前数据作用域租户标识。
	/// </summary>
	string? CurrentTenantId { get; }

	/// <summary>
	/// 获取一个值，指示是否处于全租户跨租户作用域；仅此时跳过租户过滤器。
	/// </summary>
	bool IgnoreTenantFilter { get; }
}
