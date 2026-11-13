namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 标记应用服务方法或类型需要审计。
/// </summary>
/// <remarks>
/// 未标注时按全局选项决定；标注后即使全局关闭也会记录（用于合规要求的少量方法）。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class AuditedAttribute : Attribute
{
	/// <summary>
	/// 获取或设置动作显示名称；为空时使用“服务名.方法名”。
	/// </summary>
	public string? ActionName { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否记录返回值。
	/// </summary>
	public bool RecordResult { get; set; } = true;
}
