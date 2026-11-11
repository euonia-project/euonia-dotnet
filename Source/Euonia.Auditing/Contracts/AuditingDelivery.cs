namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计投递方式。
/// </summary>
public enum AuditingDelivery
{
	/// <summary>立即写入。</summary>
	Immediate = 0,

	/// <summary>按批写入；关闭或刷写时排空。</summary>
	Batched = 1,
}
