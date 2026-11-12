namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 遥测能力的全局选项。
/// </summary>
public sealed class TelemetryOptions
{
	/// <summary>
	/// 配置节名称。
	/// </summary>
	public const string ConfigurationSection = "Euonia:Telemetry";

	/// <summary>
	/// 获取或设置一个值，指示是否启用遥测埋点；未启用时不创建 Activity/Meter 记录。
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否启用跟踪。
	/// </summary>
	public bool TracesEnabled { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否启用指标。
	/// </summary>
	public bool MetricsEnabled { get; set; } = true;

	/// <summary>
	/// 获取或设置服务名称。
	/// </summary>
	public string? ServiceName { get; set; }

	/// <summary>
	/// 获取或设置服务版本。
	/// </summary>
	public string? ServiceVersion { get; set; }

	/// <summary>
	/// 获取或设置部署环境。
	/// </summary>
	public string? Environment { get; set; }

	/// <summary>
	/// 获取或设置消息类型标签模式：<c>ShortName</c> 或 <c>FullName</c>。
	/// </summary>
	public string MessageTypeTag { get; set; } = "ShortName";

	/// <summary>
	/// 获取或设置一个值，指示是否记录用户标识标签。
	/// </summary>
	public bool RecordUserId { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否记录租户标识标签。
	/// </summary>
	public bool RecordTenantId { get; set; } = true;

	/// <summary>
	/// 获取或设置脱敏选项。
	/// </summary>
	public TelemetryRedactionOptions Redaction { get; set; } = new();

	/// <summary>
	/// 获取或设置按组件覆盖的开关。
	/// </summary>
	/// <remarks>
	/// 仅作用于通过 <c>TelemetrySource</c> 创建的埋点；组件内直接使用静态 <c>Meter</c>/<c>ActivitySource</c> 的指标不受此开关控制。
	/// </remarks>
	public Dictionary<string, TelemetryComponentOptions> Components { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
