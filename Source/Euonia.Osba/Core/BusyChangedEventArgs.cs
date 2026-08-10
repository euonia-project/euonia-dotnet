namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="BusyChangedEventHandler"/> 事件的事件参数。
/// </summary>
public class BusyChangedEventArgs : EventArgs
{
	/// <summary>
	/// 创建 <see cref="BusyChangedEventArgs"/> 对象的新实例。
	/// </summary>
	/// <param name="propertyName">繁忙值已更改的属性。</param>
	/// <param name="isBusy">新的繁忙值。</param>
	public BusyChangedEventArgs(string propertyName,bool isBusy)
	{
		PropertyName = propertyName;
		IsBusy = isBusy;
	}
	
	/// <summary>
	/// 获取一个值，指示该属性是否繁忙。
	/// </summary>
	public bool IsBusy { get; protected set; }
	
	/// <summary>
	/// 获取繁忙值已更改的属性名称。
	/// </summary>
	public string PropertyName { get; protected set; }
}