namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents that the implemented classes is trackable.
/// </summary>
public interface ITrackableObject : INotifyBusy
{
	/// <summary>
	/// Gets a value indicating whether the object is valid.
	/// </summary>
	bool IsValid { get; }

	/// <summary>
	/// Gets a value indicating whether the object is changed.
	/// </summary>
	bool IsChanged { get; }

	/// <summary>
	/// Gets a value indicating whether the object is deleted.
	/// </summary>
	bool IsDeleted { get; }

	/// <summary>
	/// Gets a value indicating whether the object is new.
	/// </summary>
	bool IsNew { get; }

	/// <summary>
	/// Gets a value indicating whether the object can be saved.
	/// </summary>
	bool IsSavable { get; }
}