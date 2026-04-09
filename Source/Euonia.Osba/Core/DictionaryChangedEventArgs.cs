namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Provides data for events that report changes to a dictionary, including the affected key, the type of change, and
/// the old and new values.
/// </summary>
/// <remarks>Use this class with dictionary change notifications to determine which entry was affected and how.
/// The event data includes the key, the action performed, and the values before and after the change.</remarks>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class DictionaryChangedEventArgs<TKey, TValue> : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the DictionaryChangedEventArgs class with the specified key, action, old value, and
	/// new value.
	/// </summary>
	/// <param name="key">The key in the dictionary that was affected by the change.</param>
	/// <param name="action">The type of change that occurred in the dictionary.</param>
	/// <param name="oldValue">The value associated with the key before the change. May be the default value for the type if the key was added.</param>
	/// <param name="newValue">The value associated with the key after the change. May be the default value for the type if the key was removed.</param>
	public DictionaryChangedEventArgs(TKey key, DictionaryChangedAction action, TValue oldValue, TValue newValue)
	{
		Key = key;
		Action = action;
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// Gets the key associated with the current element.
	/// </summary>
	public TKey Key { get; }

	/// <summary>
	/// Gets the type of change that occurred in the dictionary.
	/// </summary>
	public DictionaryChangedAction Action { get; }

	/// <summary>
	/// Gets the previous value before the change occurred.
	/// </summary>
	public TValue OldValue { get; }

	/// <summary>
	/// Gets the new value associated with the change event.
	/// </summary>
	public TValue NewValue { get; }
}
