namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The message meta data.
/// </summary>
/// <seealso cref="IDictionary{String, Object}"/>
public class MessageMetadata : IDictionary<string, object>
{
    private readonly Dictionary<string, object> _dictionary = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue(string key, out object value) => _dictionary.TryGetValue(key, out value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object this[string key]
    {
        get => _dictionary.TryGetValue(key, out var value) ? value : null;
        set => _dictionary[key] = value;
    }

    /// <summary>
    /// 
    /// </summary>
    public ICollection<string> Keys => _dictionary.Keys;

    /// <summary>
    /// 
    /// </summary>
    public ICollection<object> Values => _dictionary.Values;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Remove(KeyValuePair<string, object> item) => ((ICollection<KeyValuePair<string, object>>)_dictionary).Remove(item);

    /// <summary>
    /// 
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// 
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(string key, object value)
    {
	    _dictionary.Add(key, value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

    /// <summary>
    /// 
    /// </summary>
    public void Clear() => _dictionary.Clear();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(KeyValuePair<string, object> item) => _dictionary.Contains(item);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Remove(string key) => _dictionary.Remove(key);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="items"></param>
    /// <param name="index"></param>
    public void CopyTo(KeyValuePair<string, object>[] items, int index) => ((ICollection<KeyValuePair<string, object>>)_dictionary).CopyTo(items, index);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dictionary.GetEnumerator();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}