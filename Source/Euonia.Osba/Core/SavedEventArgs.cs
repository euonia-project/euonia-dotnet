namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示对象被保存时触发的事件。
/// </summary>
public class SavedEventArgs : EventArgs
{
	/// <summary>
	/// 初始化 <see cref="SavedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="newObject">新保存的对象实例。</param>
	public SavedEventArgs(object newObject)
	{
		NewObject = newObject;
	}

	/// <summary>
	/// 初始化 <see cref="SavedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="newObject">新保存的对象实例。</param>
	/// <param name="error">保存操作期间发生的异常；如果成功则为 <c>null</c>。</param>
	/// <param name="userState">与保存操作关联的用户定义状态信息。</param>
	public SavedEventArgs(object newObject, Exception error, object userState)
		: this(newObject)
	{
		Error = error;
		UserState = userState;
	}

	/// <summary>
	/// 获取新对象。
	/// </summary>
	public object NewObject { get; }

	/// <summary>
	/// 获取错误。
	/// </summary>
	public Exception Error { get; }

	/// <summary>
	/// 获取用户状态。
	/// </summary>
	public object UserState { get; }
}