namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计能力的全局选项。
/// </summary>
public sealed class AuditingOptions
{
	/// <summary>
	/// 配置节名称。
	/// </summary>
	public const string ConfigurationSection = "Euonia:Auditing";

	/// <summary>
	/// 获取或设置一个值，指示是否启用审计。
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// 获取或设置应用名称。
	/// </summary>
	public string? ApplicationName { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否记录匿名成功调用。
	/// </summary>
	public bool RecordAnonymous { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否记录动作参数。
	/// </summary>
	public bool RecordParameters { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否记录返回值。
	/// </summary>
	public bool RecordResult { get; set; } = true;

	/// <summary>
	/// 获取或设置触发模式。
	/// </summary>
	public AuditingTriggerMode Mode { get; set; } = AuditingTriggerMode.All;

	/// <summary>
	/// 获取或设置脱敏递归深度上限。
	/// </summary>
	public int MaxDepth { get; set; } = 8;

	/// <summary>
	/// 获取或设置成功记录的投递方式。
	/// </summary>
	public AuditingDelivery SuccessDelivery { get; set; } = AuditingDelivery.Batched;

	/// <summary>
	/// 获取或设置失败记录的投递方式。
	/// </summary>
	public AuditingDelivery FailureDelivery { get; set; } = AuditingDelivery.Immediate;

	/// <summary>
	/// 获取或设置批大小。
	/// </summary>
	public int BatchSize { get; set; } = 50;

	/// <summary>
	/// 获取或设置批刷写间隔。
	/// </summary>
	public TimeSpan BatchInterval { get; set; } = TimeSpan.FromSeconds(2);

	/// <summary>
	/// 获取或设置写入失败时的最大重试次数。
	/// </summary>
	public int WriteRetryCount { get; set; } = 1;

	/// <summary>
	/// 获取或设置写入队列容量；0 表示无界。
	/// </summary>
	public int QueueCapacity { get; set; }
}
