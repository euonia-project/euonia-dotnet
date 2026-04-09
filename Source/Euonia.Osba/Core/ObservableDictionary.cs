namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents a dictionary that provides change notifications when items are added, removed, or updated, and supports
/// suppression of change events during batch operations.
/// </summary>
/// <remarks>
/// ObservableDictionary{TKey, TValue} extends the standard Dictionary{TKey, TValue} by raising events
/// when its contents change, making it suitable for scenarios such as data binding or tracking changes in collections.
/// Change notifications can be temporarily suppressed to optimize performance during bulk updates. The class also
/// implements busy state notifications to indicate when long-running operations are in progress.
/// </remarks>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyBusy
{
	/// <summary>
	/// Gets or sets a value indicating whether the ObservableDictionary should raise change notifications when child items are modified.
	/// </summary>
	public bool RaiseItemChangedEvents { get; set; } = true;

	private DictionaryChangedEventHandler<TKey, TValue> _itemChanged;

	/// <summary>
	/// Occurs when a property value changes.
	/// </summary>
	/// <remarks>This event is typically raised by classes that implement the INotifyPropertyChanged interface to
	/// notify clients, such as data-binding clients, that a property value has changed. Handlers attached to this event
	/// receive the name of the property that changed in the PropertyChangedEventArgs parameter.</remarks>
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
	/// Event indicating that the busy status of the
	/// object has changed.
	/// </summary>
	public event BusyChangedEventHandler BusyChanged
	{
		add => _busyChanged = (BusyChangedEventHandler)Delegate.Combine(_busyChanged, value);
		remove => _busyChanged = (BusyChangedEventHandler)Delegate.Remove(_busyChanged, value);
	}

	/// <summary>
	/// Override this method to be notified when the
	/// IsBusy property has changed.
	/// </summary>
	/// <param name="args">Event arguments.</param>
	protected virtual void OnBusyChanged(BusyChangedEventArgs args)
	{
		_busyChanged?.Invoke(this, args);
	}

	/// <summary>
	/// Raises the BusyChanged event for a specific property.
	/// </summary>
	/// <param name="propertyName">Name of the property.</param>
	/// <param name="busy">New busy value.</param>
	protected void OnBusyChanged(string propertyName, bool busy)
	{
		OnBusyChanged(new BusyChangedEventArgs(propertyName, busy));
	}

	/// <summary>
	/// Gets a value indicating whether the instance is currently engaged in a long-running or background operation.
	/// </summary>
	/// <remarks>Use this property to determine if the object is busy performing an operation. This can be useful
	/// for managing user interface states, such as disabling controls or displaying progress indicators while work is in
	/// progress.</remarks>
	public virtual bool IsBusy => false;

	/// <summary>
	/// Gets a value indicating whether the current instance is busy processing tasks.
	/// </summary>
	/// <remarks>This property reflects the state of the IsBusy property, providing a convenient way to check if the
	/// instance is currently engaged in operations.</remarks>
	public virtual bool IsSelfBusy => IsBusy;

	#endregion

	#region Overriding

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <remarks>Setting this property raises a change event if the value is added or updated and differs from the
	/// existing value. If the key does not exist, a new entry is added.</remarks>
	/// <param name="key">The key whose value to get or set.</param>
	/// <returns>The value associated with the specified key.</returns>
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
	/// Adds the specified key and value to the dictionary, raising item changed events if enabled.
	/// </summary>
	/// <remarks>If item changed events are enabled, this method raises an event after the item is added. If an
	/// element with the same key already exists, an exception is thrown.</remarks>
	/// <param name="key">The key of the element to add. Cannot be null.</param>
	/// <param name="value">The value of the element to add. May be null if the dictionary allows null values.</param>
	public new void Add(TKey key, TValue value)
	{
		base.Add(key, value);
		if (RaiseItemChangedEvents)
		{
			OnItemChanged(key, DictionaryChangedAction.Add, default, value);
		}
	}

	/// <summary>
	/// Removes the element with the specified key from the dictionary.
	/// </summary>
	/// <remarks>If item change events are enabled, removing an item will raise an item changed event. The method
	/// does not throw an exception if the key does not exist.</remarks>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
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
	/// Use this object to suppress ItemChangedEvents for an entire code block.
	/// May be nested in multiple levels for the same object.
	/// </summary>
	public IDisposable SuppressItemChangedEvents => new SuppressItemChangedEventsClass(this);

	/// <summary>
	/// <![CDATA[Provides a mechanism to temporarily suppress change notifications for an ObservableDictionary<TKey, TValue> instance.]]>
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Use this class to prevent the ObservableDictionary<TKey, TValue> from raising change notifications while performing
	/// multiple updates. Change notifications are automatically restored when the instance is disposed. This is useful for
	/// improving performance and avoiding unnecessary updates to data-bound controls during batch operations.
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
