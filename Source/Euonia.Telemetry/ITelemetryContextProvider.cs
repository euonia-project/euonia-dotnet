namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 定义遥测上下文来源。
/// </summary>
public interface ITelemetryContextProvider
{
	/// <summary>
	/// 获取当前遥测上下文；不可用时为 <c>null</c>。
	/// </summary>
	/// <returns>遥测上下文。</returns>
	TelemetryContext? GetContext();
}
