using System.Collections.Specialized;
using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Provides data for events that are raised when an object or its properties change.
/// </summary>
/// <remarks>
/// This class consolidates multiple change notification types into a single event argument structure,
/// supporting property changes, collection changes, and list changes. It is commonly used in
/// scenarios where complex object graphs need to propagate change notifications up through
/// parent-child relationships.
/// </remarks>
public class ObjectChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectChangedEventArgs"/> class with the specified
	/// changed object and property change information.
	/// </summary>
	/// <param name="changedObject">The object that changed.</param>
	/// <param name="propertyChangedArgs">The <see cref="PropertyChangedEventArgs"/> containing details about the property change.</param>
	public ObjectChangedEventArgs(object changedObject, PropertyChangedEventArgs propertyChangedArgs)
	{
		ChangedObject = changedObject;
		PropertyChangedArgs = propertyChangedArgs;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectChangedEventArgs"/> class with the specified
	/// changed object, property change information, and collection change information.
	/// </summary>
	/// <param name="changedObject">The object that changed.</param>
	/// <param name="propertyChangedArgs">The <see cref="PropertyChangedEventArgs"/> containing details about the property change.</param>
	/// <param name="collectionChangedArgs">The <see cref="NotifyCollectionChangedEventArgs"/> containing details about the collection change.</param>
	public ObjectChangedEventArgs(object changedObject, PropertyChangedEventArgs propertyChangedArgs, NotifyCollectionChangedEventArgs collectionChangedArgs)
		: this(changedObject, propertyChangedArgs)
	{
		CollectionChangedArgs = collectionChangedArgs;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectChangedEventArgs"/> class with the specified
	/// changed object, property change information, and list change information.
	/// </summary>
	/// <param name="changedObject">The object that changed.</param>
	/// <param name="propertyChangedArgs">The <see cref="PropertyChangedEventArgs"/> containing details about the property change.</param>
	/// <param name="listChangedArgs">The <see cref="ListChangedEventArgs"/> containing details about the list change.</param>
	public ObjectChangedEventArgs(object changedObject, PropertyChangedEventArgs propertyChangedArgs, ListChangedEventArgs listChangedArgs)
		: this(changedObject, propertyChangedArgs)
	{
		ListChangedArgs = listChangedArgs;
	}

	/// <summary>
	/// Gets the object that changed.
	/// </summary>
	/// <value>
	/// The object instance that triggered the change notification.
	/// </value>
	public object ChangedObject { get; }

	/// <summary>
	/// Gets the property change event arguments.
	/// </summary>
	/// <value>
	/// A <see cref="PropertyChangedEventArgs"/> instance containing information about the property that changed,
	/// or <see langword="null"/> if the change was not property-related.
	/// </value>
	public PropertyChangedEventArgs PropertyChangedArgs { get; }

	/// <summary>
	/// Gets the collection change event arguments.
	/// </summary>
	/// <value>
	/// A <see cref="NotifyCollectionChangedEventArgs"/> instance containing information about the collection change,
	/// or <see langword="null"/> if the change was not collection-related.
	/// </value>
	public NotifyCollectionChangedEventArgs CollectionChangedArgs { get; }

	/// <summary>
	/// Gets the list change event arguments.
	/// </summary>
	/// <value>
	/// A <see cref="ListChangedEventArgs"/> instance containing information about the list change,
	/// or <see langword="null"/> if the change was not list-related.
	/// </value>
	public ListChangedEventArgs ListChangedArgs { get; }
}