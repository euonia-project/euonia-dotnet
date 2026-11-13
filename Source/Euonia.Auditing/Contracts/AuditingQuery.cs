namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计记录查询条件。
/// </summary>
public sealed class AuditingQuery
{
	/// <summary>
	/// 获取或设置租户标识。
	/// </summary>
	public string? TenantId { get; set; }

	/// <summary>
	/// 获取或设置用户标识。
	/// </summary>
	public string? UserId { get; set; }

	/// <summary>
	/// 获取或设置关联标识。
	/// </summary>
	public string? CorrelationId { get; set; }

	/// <summary>
	/// 获取或设置是否成功过滤；<c>null</c> 表示不过滤。
	/// </summary>
	public bool? IsSuccess { get; set; }

	/// <summary>
	/// 获取或设置起始时间（含）。
	/// </summary>
	public DateTime? From { get; set; }

	/// <summary>
	/// 获取或设置结束时间（不含）。
	/// </summary>
	public DateTime? To { get; set; }

	/// <summary>
	/// 获取或设置跳过的记录数。
	/// </summary>
	public int Skip { get; set; }

	/// <summary>
	/// 获取或设置是否允许跨租户查询；为 <c>true</c> 时必须提供 <see cref="CrossTenantReason"/>。
	/// </summary>
	public bool AllowCrossTenant { get; set; }

	/// <summary>
	/// 获取或设置跨租户查询原因（用于审计/合规）。
	/// </summary>
	public string? CrossTenantReason { get; set; }

	/// <summary>
	/// 获取或设置返回的最大记录数。
	/// </summary>
	public int Take { get; set; } = 50;
}
