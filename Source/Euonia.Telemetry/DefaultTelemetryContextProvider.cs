using System.Diagnostics;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 默认上下文提供程序：<see cref="Activity.Current"/> 优先，其次执行上下文；不生成新标识。
/// </summary>
public sealed class DefaultTelemetryContextProvider : ITelemetryContextProvider
{
	private readonly IExecutionContextAccessor? _executionContextAccessor;

	/// <summary>
	/// 初始化 <see cref="DefaultTelemetryContextProvider"/> 的新实例。
	/// </summary>
	/// <param name="executionContextAccessor">执行上下文访问器；可选。</param>
	public DefaultTelemetryContextProvider(IExecutionContextAccessor? executionContextAccessor = null)
	{
		_executionContextAccessor = executionContextAccessor;
	}

	/// <inheritdoc />
	public TelemetryContext? GetContext()
	{
		var execution = _executionContextAccessor?.Current ?? AmbientExecutionContext.Current;
		var activity = Activity.Current;

		if (execution == null && activity == null)
		{
			return null;
		}

		return new TelemetryContext(
			activity?.TraceId.ToHexString(),
			activity?.SpanId.ToHexString(),
			execution?.CorrelationId,
			execution?.RequestTraceId,
			execution?.TenantId,
			execution?.ActorTenantId,
			execution?.UserId,
			execution?.IsCrossTenant ?? false);
	}
}
