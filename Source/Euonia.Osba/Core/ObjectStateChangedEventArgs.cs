namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 对象编辑状态变化的事件参数。
/// </summary>
public sealed class ObjectStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// 初始化 <see cref="ObjectStateChangedEventArgs"/> 的新实例。
	/// </summary>
	/// <param name="oldState">变化前的状态。</param>
	/// <param name="newState">变化后的状态。</param>
	public ObjectStateChangedEventArgs(ObjectEditState oldState, ObjectEditState newState)
	{
		OldState = oldState;
		NewState = newState;
	}

	/// <summary>
	/// 获取变化前的状态。
	/// </summary>
	public ObjectEditState OldState { get; }

	/// <summary>
	/// 获取变化后的状态。
	/// </summary>
	public ObjectEditState NewState { get; }
}
