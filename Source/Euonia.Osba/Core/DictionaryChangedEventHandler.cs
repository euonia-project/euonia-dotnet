namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents the method that handles dictionary change notifications for a dictionary with keys of type TKey and
/// values of type TValue.
/// </summary>
/// <remarks>Use this delegate to subscribe to events that notify when items are added, removed, or updated in a
/// dictionary. The event data provides details about the change.</remarks>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="args">A DictionaryChangedEventArgs{TKey, TValue} that contains the event data describing the change.</param>
public delegate void DictionaryChangedEventHandler<TKey, TValue>(object sender, DictionaryChangedEventArgs<TKey, TValue> args);
