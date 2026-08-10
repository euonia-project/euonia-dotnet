namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示实现此接口的类是可跟踪的。
/// </summary>
public interface ITrackableObject : INotifyBusy
{
	/// <summary>
	/// 获取一个值，指示对象是否有效。
	/// </summary>
	bool IsValid { get; }

	/// <summary>
	/// 获取一个值，指示对象是否已更改。
	/// </summary>
	bool IsChanged { get; }

	/// <summary>
	/// 获取一个值，指示对象是否已删除。
	/// </summary>
	bool IsDeleted { get; }

	/// <summary>
	/// 获取一个值，指示对象是否为新对象。
	/// </summary>
	bool IsNew { get; }

	/// <summary>
	/// 获取一个值，指示对象是否可保存。
	/// </summary>
	bool IsSavable { get; }
}