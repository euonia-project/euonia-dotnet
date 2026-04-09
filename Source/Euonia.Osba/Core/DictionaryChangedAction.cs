namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Specifies the type of change that has occurred in a dictionary, such as adding, removing, or replacing items.
/// </summary>
/// <remarks>Use this enumeration to determine the nature of a change when handling dictionary change
/// notifications, such as in observable or event-driven dictionary implementations.</remarks>
public enum DictionaryChangedAction
{
	/// <summary>
	/// Indicates that an item was added to the dictionary. The event arguments will typically include the key and value of the added item.
	/// </summary>
	Add,
	/// <summary>
	/// Indicates that an item was removed from the dictionary. The event arguments will typically include the key and value of the removed item.
	/// </summary>
	Remove,
	/// <summary>
	/// Replaces all occurrences of a specified value with another value.
	/// </summary>
	Update,
}
