namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 只读遥测上下文；仅用于标签与日志，不参与授权或业务判断。
/// </summary>
/// <param name="TraceId">W3C Trace 标识。</param>
/// <param name="SpanId">W3C Span 标识。</param>
/// <param name="CorrelationId">业务关联标识。</param>
/// <param name="RequestTraceId">请求追踪标识。</param>
/// <param name="TenantId">数据作用域租户。</param>
/// <param name="ActorTenantId">操作人租户。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="IsCrossTenant">是否处于跨租户作用域。</param>
public sealed record TelemetryContext(
	string? TraceId,
	string? SpanId,
	string? CorrelationId,
	string? RequestTraceId,
	string? TenantId,
	string? ActorTenantId,
	string? UserId,
	bool IsCrossTenant);
