namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 当前异步流的租户快照。
/// </summary>
public sealed class TenantContext
{
	/// <summary>
	/// 获取数据作用域租户标识；全租户跨租户作用域时为 <c>null</c>。
	/// </summary>
	public string? TenantId { get; init; }

	/// <summary>
	/// 获取操作人所属租户标识。
	/// </summary>
	public string? ActorTenantId { get; init; }

	/// <summary>
	/// 获取跨租户目标租户标识。
	/// </summary>
	public string? TargetTenantId { get; init; }

	/// <summary>
	/// 获取租户完整信息；消息/后台路径可能只有标识。
	/// </summary>
	public ITenantInfo? Tenant { get; init; }

	/// <summary>
	/// 获取一个值，指示当前是否处于显式跨租户作用域。
	/// </summary>
	public bool IsCrossTenant { get; init; }

	/// <summary>
	/// 获取跨租户原因。
	/// </summary>
	public string? CrossTenantReason { get; init; }
}
