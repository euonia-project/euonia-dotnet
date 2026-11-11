namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 遥测标签脱敏选项。
/// </summary>
public sealed class TelemetryRedactionOptions
{
	/// <summary>
	/// 获取或设置一个值，指示是否启用脱敏。
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 获取或设置需要脱敏的标签键。
	/// </summary>
	public string[] Keys { get; set; } =
	[
		"http.request.header.authorization",
		"http.request.header.cookie",
		"db.statement",
		"messaging.message.payload",
		"exception.message",
	];
}
