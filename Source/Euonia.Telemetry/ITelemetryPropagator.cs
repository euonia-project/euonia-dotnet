using System.Diagnostics;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 定义 W3C 跟踪上下文注入与提取契约。
/// </summary>
public interface ITelemetryPropagator
{
	/// <summary>
	/// 把当前活动的跟踪上下文注入载体。
	/// </summary>
	/// <param name="carrier">目标载体。</param>
	/// <param name="activity">要注入的活动；为 <c>null</c> 时使用 <see cref="Activity.Current"/>。</param>
	void Inject(IDictionary<string, object> carrier, Activity? activity = null);

	/// <summary>
	/// 从载体提取远端跟踪上下文。
	/// </summary>
	/// <param name="carrier">来源载体。</param>
	/// <returns>远端上下文；非法或缺失时为 <c>null</c>（按新根处理）。</returns>
	ActivityContext? Extract(IDictionary<string, object> carrier);
}
