namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示实现此接口的类可以通知繁忙状态。
/// </summary>
public interface INotifyBusy
{
	/// <summary>
	/// 当对象的繁忙状态改变时发生。
	/// </summary>
	event BusyChangedEventHandler BusyChanged;
	
	/// <summary>
	/// 获取一个值，指示对象或其任何子对象是否繁忙。
	/// </summary>
	bool IsBusy { get; }
	
	/// <summary>
	/// 获取一个值，指示对象本身是否繁忙。
	/// </summary>
	bool IsSelfBusy { get; }
}