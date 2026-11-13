namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 单个组件的遥测开关。
/// </summary>
public sealed class TelemetryComponentOptions
{
	/// <summary>
	/// 获取或设置一个值，指示是否启用该组件。
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否启用跟踪。
	/// </summary>
	public bool TracesEnabled { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否启用指标。
	/// </summary>
	public bool MetricsEnabled { get; set; } = true;
}
