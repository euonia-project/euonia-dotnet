namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Interface for field data.
/// </summary>
public interface IFieldData : ITrackableObject
{
	/// <summary>
	/// Gets the name of the field.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets or sets the field value.
	/// </summary>
	/// <value>The value of the field.</value>
	/// <returns>The value of the field.</returns>
	object Value { get; set; }

	/// <summary>
	/// Marks the field as unchanged.
	/// </summary>
	void MarkAsUnchanged();

	/// <summary>
	/// Restore value to previous one.
	/// </summary>
	void Undo();
}

/// <summary>
/// Interface for field data of a specific type.
/// </summary>
/// <typeparam name="T">The type of the field data.</typeparam>
public interface IFieldData<T> : IFieldData
{
	/// <summary>
	/// Gets or sets the field value.
	/// </summary>
	/// <value>The value of the field.</value>
	/// <returns>The value of the field.</returns>
	new T Value { get; set; }
}