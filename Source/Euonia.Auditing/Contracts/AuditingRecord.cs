namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 一次执行作用域（HTTP 请求 / 消息 / 后台作业）的审计记录。
/// </summary>
public sealed class AuditingRecord
{
	/// <summary>
	/// 获取或设置记录标识（UUID v7）。
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// 获取或设置作用域标识；全局唯一，格式为 <c>{actorTenantId ?? "-"}:{opKey}</c>。
	/// </summary>
	public string? ScopeId { get; set; }

	/// <summary>
	/// 获取或设置租户标识；优先操作人租户，匿名时回退数据租户。
	/// </summary>
	public string? TenantId { get; set; }

	/// <summary>
	/// 获取或设置租户名称。
	/// </summary>
	public string? TenantName { get; set; }

	/// <summary>
	/// 获取或设置跨租户目标租户标识。
	/// </summary>
	public string? TargetTenantId { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否处于显式跨租户作用域。
	/// </summary>
	public bool IsCrossTenant { get; set; }

	/// <summary>
	/// 获取或设置跨租户原因。
	/// </summary>
	public string? CrossTenantReason { get; set; }

	/// <summary>
	/// 获取或设置用户标识。
	/// </summary>
	public string? UserId { get; set; }

	/// <summary>
	/// 获取或设置用户名称。
	/// </summary>
	public string? UserName { get; set; }

	/// <summary>
	/// 获取或设置客户端标识。
	/// </summary>
	public string? ClientId { get; set; }

	/// <summary>
	/// 获取或设置关联标识。
	/// </summary>
	public string? CorrelationId { get; set; }

	/// <summary>
	/// 获取或设置 W3C 跟踪标识。
	/// </summary>
	public string? TraceId { get; set; }

	/// <summary>
	/// 获取或设置 W3C Span 标识。
	/// </summary>
	public string? SpanId { get; set; }

	/// <summary>
	/// 获取或设置请求追踪标识。
	/// </summary>
	public string? RequestTraceId { get; set; }

	/// <summary>
	/// 获取或设置工作单元标识。
	/// </summary>
	public string? UnitOfWorkId { get; set; }

	/// <summary>
	/// 获取或设置应用名称。
	/// </summary>
	public string? ApplicationName { get; set; }

	/// <summary>
	/// 获取或设置开始执行时间（UTC）。
	/// </summary>
	public DateTime ExecutionTime { get; set; }

	/// <summary>
	/// 获取或设置执行耗时（毫秒）。
	/// </summary>
	public int ExecutionDuration { get; set; }

	/// <summary>
	/// 获取或设置客户端 IP。
	/// </summary>
	public string? ClientIpAddress { get; set; }

	/// <summary>
	/// 获取或设置客户端名称。
	/// </summary>
	public string? ClientName { get; set; }

	/// <summary>
	/// 获取或设置浏览器信息。
	/// </summary>
	public string? BrowserInfo { get; set; }

	/// <summary>
	/// 获取或设置 HTTP 方法。
	/// </summary>
	public string? HttpMethod { get; set; }

	/// <summary>
	/// 获取或设置 HTTP 状态码。
	/// </summary>
	public int? HttpStatusCode { get; set; }

	/// <summary>
	/// 获取或设置请求 URL（不含查询串）。
	/// </summary>
	public string? Url { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示作用域是否成功；<c>null</c> 表示未知。
	/// </summary>
	public bool? IsSuccess { get; set; }

	/// <summary>
	/// 获取或设置扩展属性 JSON。
	/// </summary>
	public string? ExtraProperties { get; set; }

	/// <summary>
	/// 获取或设置写入器接收时间（UTC）。
	/// </summary>
	public DateTime RecordedAt { get; set; }

	/// <summary>
	/// 获取或设置作用域内的动作列表；允许为空。
	/// </summary>
	public List<AuditingAction> Actions { get; set; } = [];
}
