using System.Diagnostics;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 基于 <see cref="DistributedContextPropagator.Current"/> 的默认传播器，不依赖 OTel SDK。
/// </summary>
public sealed class DefaultTelemetryPropagator : ITelemetryPropagator
{
	/// <inheritdoc />
	public void Inject(IDictionary<string, object> carrier, Activity? activity = null)
	{
		ArgumentNullException.ThrowIfNull(carrier);

		activity ??= Activity.Current;
		if (activity == null)
		{
			return;
		}

		DistributedContextPropagator.Current.Inject(
			activity,
			carrier,
			static (object? currentCarrier, string key, string value) =>
			{
				if (!string.IsNullOrEmpty(value))
				{
					((IDictionary<string, object>)currentCarrier!)[key] = value;
				}
			});
	}

	/// <inheritdoc />
	public ActivityContext? Extract(IDictionary<string, object> carrier)
	{
		ArgumentNullException.ThrowIfNull(carrier);

		DistributedContextPropagator.Current.ExtractTraceIdAndState(
			carrier,
			static (object? currentCarrier, string key, out string? value, out IEnumerable<string>? values) =>
			{
				value = ((IDictionary<string, object>)currentCarrier!).TryGetValue(key, out var entry)
					        ? entry as string
					        : null;
				values = null;
			},
			out var traceParent,
			out var traceState);

		if (string.IsNullOrWhiteSpace(traceParent)
		    || !ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var context))
		{
			// 非法 traceparent 不抛异常：按新根 Trace 处理。
			return null;
		}

		return context;
	}
}
