using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents a collection of objects that notifies listeners of dynamic changes, such as when items are added,
/// removed, or the entire list is refreshed, and also provides notifications for changes in the busy state of the
/// collection or its items.
/// </summary>
/// <remarks>
/// ObservableList{T} extends ObservableCollection{T} by providing additional support for monitoring
/// changes in child objects and busy state notifications. This makes it suitable for data binding scenarios where UI
/// elements need to stay synchronized with the underlying data and be aware of long-running operations. The collection
/// can suppress change notifications during batch updates to improve performance and avoid unnecessary UI refreshes. It
/// also propagates property and busy state changes from child items, enabling more granular change tracking.
/// </remarks>
/// <typeparam name="TItem">The type of elements contained in the observable list.</typeparam>
public class ObservableList<TItem> : ObservableCollection<TItem>, INotifyBusy
{
	private EventHandler<ObjectChangedEventArgs> _childChanged = null;

	/// <summary>
	/// Occurs when a child object in the collection is changed, providing details about the change through event
	/// arguments.
	/// </summary>
	/// <remarks>Subscribe to this event to be notified when a child object is updated, removed, or otherwise
	/// modified. The associated ObjectChangedEventArgs instance contains information about the specific change, such as
	/// the affected object and the type of change. Event handlers should examine the event arguments to determine the
	/// nature of the change and respond appropriately.</remarks>
	public event EventHandler<ObjectChangedEventArgs> ChildChanged
	{
		add => _childChanged = (EventHandler<ObjectChangedEventArgs>)Delegate.Combine(_childChanged, value);
		remove => _childChanged = (EventHandler<ObjectChangedEventArgs>)Delegate.Remove(_childChanged, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the list raises change notification events when its contents are modified.
	/// </summary>
	/// <remarks>When set to <see langword="true"/>, the list notifies subscribers of changes such as additions,
	/// removals, or updates. This is typically used to support data binding scenarios where user interface elements need
	/// to stay synchronized with the underlying data. Setting this property to <see langword="false"/> suppresses these
	/// notifications, which can improve performance when making bulk updates.</remarks>
	public bool RaiseListChangedEvents { get; set; } = true;

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

	/// <summary>
	/// Removes the item at the specified index from the collection and detaches any associated event handlers.
	/// </summary>
	/// <remarks>Overrides the base implementation to ensure that event handlers are properly removed before the
	/// item is deleted. An exception is thrown if the specified index is out of range.</remarks>
	/// <param name="index">The zero-based index of the item to remove from the collection.</param>
	protected override void RemoveItem(int index)
	{
		RemoveEventHooks(this[index]);
		base.RemoveItem(index);
	}

	/// <summary>
	/// Inserts the specified item into the collection at the given index and attaches event handlers to the item.
	/// </summary>
	/// <remarks>This method overrides the base implementation to ensure that event hooks are added to the item
	/// after insertion. This allows the collection to respond to events raised by the item.</remarks>
	/// <param name="index">The zero-based index at which the item should be inserted into the collection.</param>
	/// <param name="item">The item to insert and attach event hooks to.</param>
	protected override void InsertItem(int index, TItem item)
	{
		base.InsertItem(index, item);
		AddEventHooks(item);
	}

	/// <summary>
	/// Raises the collection changed event when the collection is modified, if list change notifications are enabled.
	/// </summary>
	/// <remarks>Overrides the base implementation to conditionally raise collection change notifications based on
	/// the value of the RaiseListChangedEvents property. If RaiseListChangedEvents is <see langword="true"/>, the base
	/// event is raised; otherwise, no notification is sent.</remarks>
	/// <param name="e">An object that contains information about the change that occurred in the collection.</param>
	protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		if (RaiseListChangedEvents)
		{
			base.OnCollectionChanged(e);
		}
	}

	/// <summary>
	/// Removes event hooks from an item.
	/// </summary>
	/// <param name="item"></param>
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
	/// Adds event handlers to the specified item to monitor changes in its busy state and property values.
	/// </summary>
	/// <remarks>This method subscribes to the BusyChanged event if the item implements INotifyBusy, and to the
	/// PropertyChanged event if the item implements INotifyPropertyChanged. These subscriptions enable the system to
	/// respond to changes in the item's state or properties.</remarks>
	/// <param name="item">The item to which event handlers are added. This parameter must not be null; if null, no handlers are attached.</param>
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
	/// Raises the event that notifies subscribers when a child object has changed.
	/// </summary>
	/// <remarks>Override this method in a derived class to provide custom handling when a child object changes.
	/// Ensure that event subscribers are managed appropriately to prevent memory leaks.</remarks>
	/// <param name="args">An <see cref="ObjectChangedEventArgs"/> instance that contains information about the change to the child object.</param>
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
	/// Use this object to suppress ListChangedEvents for an entire code block.
	/// May be nested in multiple levels for the same object.
	/// </summary>
	public IDisposable SuppressListChangedEvents => new SuppressListChangedEventsClass<TItem>(this);

	/// <summary>
	/// <![CDATA[Provides a mechanism to temporarily suppress change notifications for an ObservableList<T> instance.]]>
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Use this class to prevent the ObservableList<T> from raising change notifications while performing
	/// multiple updates. Change notifications are automatically restored when the instance is disposed. This is useful for
	/// improving performance and avoiding unnecessary updates to data-bound controls during batch operations.
	/// ]]>
	/// </remarks>
	/// <typeparam name="TList">The type of elements contained in the observable list.</typeparam>
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