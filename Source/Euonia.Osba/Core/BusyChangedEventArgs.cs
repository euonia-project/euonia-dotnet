namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Event arguments for the <see cref="EditableObject{T}.BusyChanged"/> event.
/// </summary>
public class BusyChangedEventArgs : EventArgs
{
	/// <summary>
	/// Create a new instance of the <see cref="BusyChangedEventArgs"/> object.
	/// </summary>
	/// <param name="propertyName">The property for which the busy value has changed.</param>
	/// <param name="isBusy">New busy value.</param>
	public BusyChangedEventArgs(string propertyName,bool isBusy)
	{
		PropertyName = propertyName;
		IsBusy = isBusy;
	}
	
	/// <summary>
	/// Gets a value indicating whether the property is busy.
	/// </summary>
	public bool IsBusy { get; protected set; }
	
	/// <summary>
	/// Gets the name of the property for which the Busy value has changed.
	/// </summary>
	public string PropertyName { get; protected set; }
}