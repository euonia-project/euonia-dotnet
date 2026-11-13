namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计触发模式。
/// </summary>
public enum AuditingTriggerMode
{
	/// <summary>记录所有被拦截的调用。</summary>
	All = 0,

	/// <summary>只记录写操作；读前缀方法跳过（<see cref="AuditedAttribute"/> 可覆盖）。</summary>
	WritesOnly = 1,
}
