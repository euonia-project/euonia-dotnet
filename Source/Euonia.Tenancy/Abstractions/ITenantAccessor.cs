namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义读取当前租户上下文的契约。
/// </summary>
public interface ITenantAccessor
{
	/// <summary>
	/// 获取当前租户快照；未安装租户作用域时为 <c>null</c>。
	/// </summary>
	TenantContext? Current { get; }

	/// <summary>
	/// 获取当前数据作用域租户标识。
	/// </summary>
	string? TenantId { get; }
}
