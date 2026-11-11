using System.Diagnostics;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 定义 Activity 创建后的标签补充器。
/// </summary>
public interface ITelemetryEnricher
{
	/// <summary>
	/// 为活动补充标签。
	/// </summary>
	/// <param name="activity">目标活动。</param>
	/// <param name="context">遥测上下文。</param>
	void Enrich(Activity activity, TelemetryContext? context);
}
