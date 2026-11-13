using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 日志 Scope 写入 TraceId / SpanId / CorrelationId / TenantId。
/// </summary>
public static class TelemetryLogScope
{
	/// <summary>
	/// 开始日志 Scope。
	/// </summary>
	/// <param name="logger">日志记录器。</param>
	/// <param name="contextProvider">上下文提供程序；为 <c>null</c> 时不写入任何字段。</param>
	/// <returns>释放时结束 Scope；无上下文时返回空句柄。</returns>
	public static IDisposable Begin(ILogger logger, ITelemetryContextProvider? contextProvider)
	{
		ArgumentNullException.ThrowIfNull(logger);

		var context = contextProvider?.GetContext();
		if (context == null)
		{
			return EmptyScope.Instance;
		}

		var values = new Dictionary<string, object?>(StringComparer.Ordinal);
		Add(values, "TraceId", context.TraceId);
		Add(values, "SpanId", context.SpanId);
		Add(values, "CorrelationId", context.CorrelationId);
		Add(values, "TenantId", context.TenantId);
		Add(values, "RequestTraceId", context.RequestTraceId);

		return values.Count == 0 ? EmptyScope.Instance : logger.BeginScope(values) ?? EmptyScope.Instance;
	}

	private static void Add(Dictionary<string, object?> values, string key, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			values[key] = value;
		}
	}

	private sealed class EmptyScope : IDisposable
	{
		public static readonly EmptyScope Instance = new();

		public void Dispose()
		{
		}
	}
}
