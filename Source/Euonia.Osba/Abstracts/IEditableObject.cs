namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents an object which can be edited.
/// </summary>
public interface IEditableObject : IBusinessObject, ITrackableObject
{
	/// <summary>
	/// Gets the current object state.
	/// </summary>
	ObjectEditState State { get; }

	/// <summary>
	/// Gets a value indicating whether to check object rules on delete.
	/// </summary>
	bool CheckObjectRulesOnDelete { get; }

	/// <summary>
	/// Marks the object as insert.
	/// </summary>
	void MarkAsNew();

	/// <summary>
	/// Marks the object as updated.
	/// </summary>
	void MarkAsChanged();

	/// <summary>
	/// Marks the object as deleted.
	/// </summary>
	void MarkAsDeleted(bool checkObjectRules = false);
}