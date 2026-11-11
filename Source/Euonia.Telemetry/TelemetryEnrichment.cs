using System.Diagnostics;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 默认标签补充：租户与关联标识只进 Trace，不进指标。
/// </summary>
public sealed class TelemetryContextEnricher : ITelemetryEnricher
{
	private readonly TelemetryOptions _options;
	private readonly IExecutionContextAccessor? _accessor;

	/// <summary>
	/// 初始化 <see cref="TelemetryContextEnricher"/> 的新实例。
	/// </summary>
	/// <param name="options">遥测选项。</param>
	/// <param name="accessor">执行上下文访问器；可选。</param>
	public TelemetryContextEnricher(IOptions<TelemetryOptions> options, IExecutionContextAccessor? accessor = null)
	{
		_options = options.Value;
		_accessor = accessor;
	}

	/// <inheritdoc />
	public void Enrich(Activity activity, TelemetryContext? context)
	{
		context ??= BuildContext();

		if (context != null)
		{
			SetIfPresent(activity, TelemetryNames.CorrelationId, context.CorrelationId);
			SetIfPresent(activity, TelemetryNames.RequestTraceId, context.RequestTraceId);

			if (_options.RecordTenantId)
			{
				SetIfPresent(activity, TelemetryNames.TenantId, context.TenantId);
				SetIfPresent(activity, TelemetryNames.TenantActorId, context.ActorTenantId);

				if (context.IsCrossTenant)
				{
					activity.SetTag(TelemetryNames.TenantCross, true);
				}
			}

			if (_options.RecordUserId)
			{
				SetIfPresent(activity, TelemetryNames.UserId, context.UserId);
			}
		}

		if (!string.IsNullOrWhiteSpace(_options.ServiceName))
		{
			activity.SetTag("service.name", _options.ServiceName);
		}
	}

	private TelemetryContext? BuildContext()
	{
		var execution = _accessor?.Current ?? AmbientExecutionContext.Current;
		if (execution == null)
		{
			return null;
		}

		return new TelemetryContext(
			Activity.Current?.TraceId.ToHexString(),
			Activity.Current?.SpanId.ToHexString(),
			execution.CorrelationId,
			execution.RequestTraceId,
			execution.TenantId,
			execution.ActorTenantId,
			execution.UserId,
			execution.IsCrossTenant);
	}

	private static void SetIfPresent(Activity activity, string key, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			activity.SetTag(key, value);
		}
	}
}
