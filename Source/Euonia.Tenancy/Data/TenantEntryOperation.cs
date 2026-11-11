namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 实体写入操作类型。
/// </summary>
public enum TenantEntryOperation
{
	/// <summary>无操作。</summary>
	None = 0,

	/// <summary>新增。</summary>
	Added = 1,

	/// <summary>修改。</summary>
	Modified = 2,

	/// <summary>删除。</summary>
	Deleted = 3,
}
