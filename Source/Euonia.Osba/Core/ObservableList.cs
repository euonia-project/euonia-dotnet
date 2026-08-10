using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示一个对象集合，当项被添加、移除或整个列表被刷新时通知侦听器动态更改，
/// 并提供集合或其项繁忙状态更改的通知。
/// </summary>
/// <remarks>
/// ObservableList{T} 通过提供对子对象更改和繁忙状态通知的额外支持来扩展 ObservableCollection{T}。
/// 这使得它适用于 UI 元素需要与底层数据保持同步并感知长时间运行操作的数据绑定场景。
/// 该集合可以在批量更新期间抑制更改通知，以提高性能并避免不必要的 UI 刷新。
/// 它还传播子项的属性和繁忙状态更改，实现更细粒度的更改跟踪。
/// </remarks>
/// <typeparam name="TItem">可观察列表中包含的元素类型。</typeparam>
public class ObservableList<TItem> : ObservableCollection<TItem>, INotifyBusy
{
	private EventHandler<ObjectChangedEventArgs> _childChanged = null;

	/// <summary>
	/// 当集合中的子对象被更改时发生，通过事件参数提供有关更改的详细信息。
	/// </summary>
	/// <remarks>订阅此事件可在子对象被更新、移除或以其他方式修改时收到通知。
	/// 关联的 ObjectChangedEventArgs 实例包含有关特定更改的信息，例如受影响的对象和更改类型。
	/// 事件处理程序应检查事件参数以确定更改的性质并做出相应响应。</remarks>
	public event EventHandler<ObjectChangedEventArgs> ChildChanged
	{
		add => _childChanged = (EventHandler<ObjectChangedEventArgs>)Delegate.Combine(_childChanged, value);
		remove => _childChanged = (EventHandler<ObjectChangedEventArgs>)Delegate.Remove(_childChanged, value);
	}

	/// <summary>
	/// 获取或设置一个值，指示列表在其内容被修改时是否引发更改通知事件。
	/// </summary>
	/// <remarks>当设置为 <see langword="true"/> 时，列表会通知订阅者添加、移除或更新等更改。
	/// 这通常用于支持用户界面元素需要与底层数据保持同步的数据绑定场景。
	/// 将此属性设置为 <see langword="false"/> 会抑制这些通知，这可以在进行批量更新时提高性能。</remarks>
	public bool RaiseListChangedEvents { get; set; } = true;

	#region BusyChanged

	private BusyChangedEventHandler _busyChanged;

	/// <summary>
	/// 指示对象繁忙状态已改变的事件。
	/// </summary>
	public event BusyChangedEventHandler BusyChanged
	{
		add => _busyChanged = (BusyChangedEventHandler)Delegate.Combine(_busyChanged, value);
		remove => _busyChanged = (BusyChangedEventHandler)Delegate.Remove(_busyChanged, value);
	}

	/// <summary>
	/// 重写此方法以在 IsBusy 属性改变时收到通知。
	/// </summary>
	/// <param name="args">事件参数。</param>
	protected virtual void OnBusyChanged(BusyChangedEventArgs args)
	{
		_busyChanged?.Invoke(this, args);
	}

	/// <summary>
	/// 为特定属性引发 BusyChanged 事件。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="busy">新的繁忙值。</param>
	protected void OnBusyChanged(string propertyName, bool busy)
	{
		OnBusyChanged(new BusyChangedEventArgs(propertyName, busy));
	}

	/// <summary>
	/// 获取一个值，指示实例当前是否正在执行长时间运行或后台操作。
	/// </summary>
	/// <remarks>使用此属性确定对象是否正在执行操作。这对于管理用户界面状态很有用，
	/// 例如在操作进行中禁用控件或显示进度指示器。</remarks>
	public virtual bool IsBusy => false;

	/// <summary>
	/// 获取一个值，指示当前实例是否正忙于处理任务。
	/// </summary>
	/// <remarks>此属性反映 IsBusy 属性的状态，提供一种便捷的方式来检查实例当前是否正在执行操作。</remarks>
	public virtual bool IsSelfBusy => IsBusy;

	#endregion

	/// <summary>
	/// 从集合中移除指定索引处的项，并分离任何关联的事件处理程序。
	/// </summary>
	/// <remarks>重写基础实现，以确保在删除项之前正确移除事件处理程序。
	/// 如果指定的索引超出范围，则抛出异常。</remarks>
	/// <param name="index">要从集合中移除的项的从零开始的索引。</param>
	protected override void RemoveItem(int index)
	{
		RemoveEventHooks(this[index]);
		base.RemoveItem(index);
	}

	/// <summary>
	/// 在给定索引处将指定项插入集合，并向该项附加事件处理程序。
	/// </summary>
	/// <remarks>此方法重写基础实现，以确保在插入后向项添加事件挂钩。
	/// 这允许集合响应由该项引发的事件。</remarks>
	/// <param name="index">应将项插入集合的从零开始的索引。</param>
	/// <param name="item">要插入并附加事件挂钩的项。</param>
	protected override void InsertItem(int index, TItem item)
	{
		base.InsertItem(index, item);
		AddEventHooks(item);
	}

	/// <summary>
	/// 在集合被修改时引发集合更改事件（如果启用了列表更改通知）。
	/// </summary>
	/// <remarks>重写基础实现，根据 RaiseListChangedEvents 属性的值有条件地引发集合更改通知。
	/// 如果 RaiseListChangedEvents 为 <see langword="true"/>，则引发基础事件；否则不发送任何通知。</remarks>
	/// <param name="e">包含集合中发生的更改信息的对象。</param>
	protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		if (RaiseListChangedEvents)
		{
			base.OnCollectionChanged(e);
		}
	}

	/// <summary>
	/// 从项中移除事件挂钩。
	/// </summary>
	/// <param name="item">要移除事件挂钩的项。</param>
	protected virtual void RemoveEventHooks(TItem item)
	{
		if (item == null)
		{
			return;
		}

		if (item is INotifyBusy busy)
		{
			busy.BusyChanged -= OnItemBusyChanged;
		}

		if (item is INotifyPropertyChanged notifyPropertyChanged)
		{
			notifyPropertyChanged.PropertyChanged -= OnItemPropertyChanged;
		}
	}

	/// <summary>
	/// 向指定项添加事件处理程序，以监视其繁忙状态和属性值的更改。
	/// </summary>
	/// <remarks>如果项实现 INotifyBusy，此方法订阅 BusyChanged 事件；如果项实现 INotifyPropertyChanged，
	/// 则订阅 PropertyChanged 事件。这些订阅使系统能够响应项状态或属性的更改。</remarks>
	/// <param name="item">要添加事件处理程序的项。此参数不能为 <c>null</c>；如果为 <c>null</c>，则不附加任何处理程序。</param>
	protected virtual void AddEventHooks(TItem item)
	{
		if (item == null)
		{
			return;
		}

		if (item is INotifyBusy notifyBusy)
		{
			notifyBusy.BusyChanged += OnItemBusyChanged;
		}

		if (item is INotifyPropertyChanged notifyPropertyChanged)
		{
			notifyPropertyChanged.PropertyChanged += OnItemPropertyChanged;
		}
	}

	private void RaiseChildChanged(object childObject, PropertyChangedEventArgs propertyChangedArgs, NotifyCollectionChangedEventArgs collectionChangedArgs)
	{
		var args = new ObjectChangedEventArgs(childObject, propertyChangedArgs, collectionChangedArgs);
		OnChildChanged(args);
	}

	/// <summary>
	/// 引发通知订阅者子对象已更改的事件。
	/// </summary>
	/// <remarks>在派生类中重写此方法以在子对象更改时提供自定义处理。
	/// 确保适当管理事件订阅者以防止内存泄漏。</remarks>
	/// <param name="args">包含子对象更改信息的 <see cref="ObjectChangedEventArgs"/> 实例。</param>
	protected virtual void OnChildChanged(ObjectChangedEventArgs args)
	{
		_childChanged?.Invoke(this, args);
	}

	#region Event Subscriptions

	private void OnItemBusyChanged(object sender, BusyChangedEventArgs e)
	{
		OnBusyChanged(e);
	}

	private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		RaiseChildChanged(sender, e, null);
	}

	#endregion

	/// <summary>
	/// 使用此对象在整段代码块中抑制 ListChangedEvents。
	/// 可以针对同一对象进行多层级嵌套。
	/// </summary>
	public IDisposable SuppressListChangedEvents => new SuppressListChangedEventsClass<TItem>(this);

	/// <summary>
	/// <![CDATA[为 ObservableList<T> 实例提供临时抑制更改通知的机制。]]>
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// 使用此类可防止 ObservableList<T> 在执行多次更新时引发更改通知。
	/// 当实例被释放时，更改通知会自动恢复。这对于在批量操作期间提高性能并避免对数据绑定控件进行不必要的更新很有用。
	/// ]]>
	/// </remarks>
	/// <typeparam name="TList">可观察列表中包含的元素类型。</typeparam>
	private class SuppressListChangedEventsClass<TList> : IDisposable
	{
		private readonly ObservableList<TList> _listObject;
		private readonly bool _initialRaiseListChangedEvents;

		public SuppressListChangedEventsClass(ObservableList<TList> listObject)
		{
			_listObject = listObject;
			_initialRaiseListChangedEvents = listObject.RaiseListChangedEvents;
			listObject.RaiseListChangedEvents = false;
		}

		public void Dispose()
		{
			_listObject.RaiseListChangedEvents = _initialRaiseListChangedEvents;
		}
	}
}