namespace Nerosoft.Euonia.Messaging;

/// <summary>
/// 定义 W3C 追踪上下文头名称。
/// </summary>
/// <remarks>
/// 业务关联标识仍使用 <c>MessageHeaders</c> 中的 <c>x-correlation-id</c> 等键；
/// 本类型只补充标准 W3C 追踪头，避免与业务头混用。
/// </remarks>
public static class TraceHeaders
{
	/// <summary>
	/// W3C traceparent 头。
	/// </summary>
	public const string TraceParent = "traceparent";

	/// <summary>
	/// W3C tracestate 头。
	/// </summary>
	public const string TraceState = "tracestate";

	/// <summary>
	/// W3C baggage 头。
	/// </summary>
	public const string Baggage = "baggage";
}
