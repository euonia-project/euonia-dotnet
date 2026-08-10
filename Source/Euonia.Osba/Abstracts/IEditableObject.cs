namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示一个可编辑的对象。
/// </summary>
public interface IEditableObject : IBusinessObject, ITrackableObject
{
	/// <summary>
	/// 获取当前的对象状态。
	/// </summary>
	ObjectEditState State { get; }

	/// <summary>
	/// 获取一个值，指示删除时是否检查对象规则。
	/// </summary>
	bool CheckObjectRulesOnDelete { get; }

	/// <summary>
	/// 将对象标记为新增。
	/// </summary>
	void MarkAsNew();

	/// <summary>
	/// 将对象标记为已更改。
	/// </summary>
	void MarkAsChanged();

	/// <summary>
	/// 将对象标记为已删除。
	/// </summary>
	/// <param name="checkObjectRules">是否在删除时检查对象规则。</param>
	void MarkAsDeleted(bool checkObjectRules = false);
}