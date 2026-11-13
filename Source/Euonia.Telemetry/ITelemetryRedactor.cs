namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 定义遥测标签脱敏契约。
/// </summary>
public interface ITelemetryRedactor
{
	/// <summary>
	/// 判断指定标签键是否需要脱敏。
	/// </summary>
	/// <param name="key">标签键。</param>
	/// <returns>需要脱敏时为 <see langword="true"/>。</returns>
	bool ShouldRedact(string key);

	/// <summary>
	/// 脱敏标签值。
	/// </summary>
	/// <param name="key">标签键。</param>
	/// <param name="value">标签值。</param>
	/// <returns>脱敏后的值。</returns>
	object? Redact(string key, object? value);
}
