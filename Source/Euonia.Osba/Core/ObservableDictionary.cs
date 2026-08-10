namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示一个字典，当添加、移除或更新项时提供更改通知，并支持在批量操作期间抑制更改事件。
/// </summary>
/// <remarks>
/// ObservableDictionary{TKey, TValue} 扩展了标准 Dictionary{TKey, TValue}，在其内容更改时引发事件，
/// 使其适用于数据绑定或跟踪集合更改等场景。在批量更新期间可以临时抑制更改通知以优化性能。
/// 该类还实现繁忙状态通知，以指示长时间运行的操作正在进行中。
/// </remarks>
/// <typeparam name="TKey">字典中键的类型。</typeparam>
/// <typeparam name="TValue">字典中值的类型。</typeparam>
public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyBusy
{
	/// <summary>
	/// 获取或设置一个值，指示 ObservableDictionary 在子项被修改时是否应引发更改通知。
	/// </summary>
	public bool RaiseItemChangedEvents { get; set; } = true;

	private DictionaryChangedEventHandler<TKey, TValue> _itemChanged;

	/// <summary>
	/// 当属性值更改时发生。
	/// </summary>
	/// <remarks>此事件通常由实现 INotifyPropertyChanged 接口的类引发，以通知客户端（例如数据绑定客户端）
	/// 属性值已更改。附加到此事件的处理程序会在 PropertyChangedEventArgs 参数中接收已更改属性的名称。</remarks>
	public event DictionaryChangedEventHandler<TKey, TValue> ItemChanged
	{
		add => _itemChanged = (DictionaryChangedEventHandler<TKey, TValue>)Delegate.Combine(_itemChanged, value);
		remove => _itemChanged = (DictionaryChangedEventHandler<TKey, TValue>)Delegate.Remove(_itemChanged, value);
	}

	//private void OnPropertyChanged(string propertyName)
	//{
	//	_itemChanged?.Invoke(this, new DictionaryChangedEventArgs<TKey, TValue>(default, DictionaryChangedAction.Update, default, default));
	//}

	private void OnItemChanged(TKey key, DictionaryChangedAction action, TValue oldValue, TValue newValue)
	{
		_itemChanged?.Invoke(this, new DictionaryChangedEventArgs<TKey, TValue>(key, action, oldValue, newValue));
	}

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

	#region Overriding

	/// <summary>
	/// 获取或设置与指定键关联的值。
	/// </summary>
	/// <remarks>如果添加或更新的值不同于现有值，则设置此属性会引发更改事件。
	/// 如果键不存在，则添加新条目。</remarks>
	/// <param name="key">要获取或设置其值的键。</param>
	/// <returns>与指定键关联的值。</returns>
	public new TValue this[TKey key]
	{
		get => base[key];
		set
		{
			DictionaryChangedEventArgs<TKey, TValue> eventArgs;

			if (ContainsKey(key))
			{
				var oldValue = base[key];
				if (Equals(oldValue, value))
				{
					return; // No change, so don't raise event
				}

				eventArgs = new DictionaryChangedEventArgs<TKey, TValue>(key, DictionaryChangedAction.Update, oldValue, value);
			}
			else
			{
				eventArgs = new DictionaryChangedEventArgs<TKey, TValue>(key, DictionaryChangedAction.Add, default, value);
			}
			base[key] = value;
			if (RaiseItemChangedEvents)
			{
				OnItemChanged(key, eventArgs.Action, eventArgs.OldValue, eventArgs.NewValue);
			}
		}
	}

	/// <summary>
	/// 向字典中添加指定的键和值，如果启用则在添加后引发项更改事件。
	/// </summary>
	/// <remarks>如果启用了项更改事件，此方法在添加项后会引发事件。
	/// 如果已存在相同键的元素，则抛出异常。</remarks>
	/// <param name="key">要添加元素的键。不能为 <c>null</c>。</param>
	/// <param name="value">要添加元素的值。如果字典允许 <c>null</c> 值，则可以为 <c>null</c>。</param>
	public new void Add(TKey key, TValue value)
	{
		base.Add(key, value);
		if (RaiseItemChangedEvents)
		{
			OnItemChanged(key, DictionaryChangedAction.Add, default, value);
		}
	}

	/// <summary>
	/// 从字典中移除具有指定键的元素。
	/// </summary>
	/// <remarks>如果启用了项更改事件，移除项会引发项更改事件。
	/// 如果键不存在，此方法不会抛出异常。</remarks>
	/// <param name="key">要移除元素的键。</param>
	/// <returns>如果成功找到并移除元素，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public new bool Remove(TKey key)
	{
		var result = base.Remove(key, out var value);
		if (result && RaiseItemChangedEvents)
		{
			OnItemChanged(key, DictionaryChangedAction.Remove, value, default);
		}
		return result;
	}

	#endregion

	/// <summary>
	/// 使用此对象在整段代码块中抑制 ItemChangedEvents。
	/// 可以针对同一对象进行多层级嵌套。
	/// </summary>
	public IDisposable SuppressItemChangedEvents => new SuppressItemChangedEventsClass(this);

	/// <summary>
	/// <![CDATA[为 ObservableDictionary<TKey, TValue> 实例提供临时抑制更改通知的机制。]]>
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// 使用此类可防止 ObservableDictionary<TKey, TValue> 在执行多次更新时引发更改通知。
	/// 当实例被释放时，更改通知会自动恢复。这对于在批量操作期间提高性能并避免对数据绑定控件进行不必要的更新很有用。
	/// ]]>
	/// </remarks>
	private class SuppressItemChangedEventsClass : IDisposable
	{
		private readonly ObservableDictionary<TKey, TValue> _listObject;
		private readonly bool _initialRaiseItemChangedEvents;

		public SuppressItemChangedEventsClass(ObservableDictionary<TKey, TValue> listObject)
		{
			_listObject = listObject;
			_initialRaiseItemChangedEvents = listObject.RaiseItemChangedEvents;
			listObject.RaiseItemChangedEvents = false;
		}

		public void Dispose()
		{
			_listObject.RaiseItemChangedEvents = _initialRaiseItemChangedEvents;
		}
	}
}
