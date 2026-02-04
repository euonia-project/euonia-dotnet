namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents that the implemented classes can notify busy status.
/// </summary>
public interface INotifyBusy
{
	/// <summary>
	/// Occurs when the object's busy status changes.
	/// </summary>
	event BusyChangedEventHandler BusyChanged;
	
	/// <summary>
	/// Gets a value indicating whether the object, or any of its children, is busy.
	/// </summary>
	bool IsBusy { get; }
	
	/// <summary>
	/// Gets a value indicating whether the object itself is busy.
	/// </summary>
	bool IsSelfBusy { get; }
}