namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计记录与动作的创建工具。
/// </summary>
public static class AuditingRecordFactory
{
	/// <summary>
	/// 创建 UUID v7 标识。
	/// </summary>
	/// <returns>按时间有序的 GUID。</returns>
	public static Guid NewId()
	{
		return Guid.CreateVersion7();
	}
}
