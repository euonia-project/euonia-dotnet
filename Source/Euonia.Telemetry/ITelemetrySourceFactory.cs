namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 定义组件遥测源的工厂。
/// </summary>
public interface ITelemetrySourceFactory
{
	/// <summary>
	/// 获取或创建指定组件的遥测源；同名重复调用返回同一实例。
	/// </summary>
	/// <param name="sourceName">源名称。</param>
	/// <param name="meterName">Meter 名称；默认与源名称相同。</param>
	/// <returns>遥测源。</returns>
	TelemetrySource Create(string sourceName, string? meterName = null);

	/// <summary>
	/// 获取已创建的遥测源快照。
	/// </summary>
	IReadOnlyCollection<TelemetrySource> Sources { get; }
}
