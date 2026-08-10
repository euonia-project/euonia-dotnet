namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 对象编辑状态的枚举。
/// </summary>
public enum ObjectEditState
{
	/// <summary>
	/// 无状态（对象未发生任何编辑）。
	/// </summary>
	None,

	/// <summary>
	/// 新增状态（对象为新插入）。
	/// </summary>
	New,

	/// <summary>
	/// 已更改状态（对象已更新）。
	/// </summary>
	Changed,

	/// <summary>
	/// 已删除状态（对象已删除）。
	/// </summary>
	Deleted,
}