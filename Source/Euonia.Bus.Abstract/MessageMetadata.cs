using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息元数据。
/// </summary>
/// <seealso cref="IDictionary{String, Object}"/>
public class MessageMetadata : IDictionary<string, object>
{
    private readonly Dictionary<string, object> _dictionary = new();

    /// <summary>
    /// 尝试获取指定键的元数据值。
    /// </summary>
    /// <param name="key">要查找的元数据键。</param>
    /// <param name="value">当方法返回 <c>true</c> 时为对应的元数据值；否则为 <c>null</c>。</param>
    /// <returns>若元数据中存在 <paramref name="key"/> 则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _dictionary.TryGetValue(key, out value);

    /// <summary>
    /// 获取或设置指定键的元数据。
    /// </summary>
    /// <param name="key">元数据键。</param>
    /// <remarks>
    /// 为兼容既有调用方，读取不存在的键时返回 <c>null</c> 而不是抛出
    /// <see cref="KeyNotFoundException"/>；需要区分“不存在”与“值为 null”时请使用
    /// <see cref="TryGetValue"/>。
    /// </remarks>
    public object this[string key]
    {
        [return: MaybeNull]
        get => _dictionary.TryGetValue(key, out var value) ? value : null!;
        set => _dictionary[key] = value;
    }

    /// <summary>
    /// 获取所有元数据键的集合。
    /// </summary>
    public ICollection<string> Keys => _dictionary.Keys;

    /// <summary>
    /// 获取所有元数据值的集合。
    /// </summary>
    public ICollection<object> Values => _dictionary.Values;

    /// <summary>
    /// 从元数据中移除指定的键值对。
    /// </summary>
    /// <param name="item">要移除的键值对。</param>
    /// <returns>若键存在且值与 <paramref name="item"/> 相等并被移除则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public bool Remove(KeyValuePair<string, object> item) => ((ICollection<KeyValuePair<string, object>>)_dictionary).Remove(item);

    /// <summary>
    /// 获取元数据项的数量。
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// 获取一个值，指示此元数据集合是否为只读。
    /// </summary>
    /// <remarks>此实现始终返回 <c>false</c>。</remarks>
    public bool IsReadOnly => false;

    /// <summary>
    /// 添加具有指定键和值的元数据项。
    /// </summary>
    /// <param name="key">元数据键。</param>
    /// <param name="value">与键关联的元数据值。</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">元数据中已存在相同的 <paramref name="key"/> 时抛出。</exception>
    public void Add(string key, object value)
    {
	    _dictionary.Add(key, value);
    }

    /// <summary>
    /// 添加指定的键值对。
    /// </summary>
    /// <param name="item">要添加的键值对。</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> 的键为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">元数据中已存在相同的键时抛出。</exception>
    public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

    /// <summary>
    /// 移除所有元数据项。
    /// </summary>
    public void Clear() => _dictionary.Clear();

    /// <summary>
    /// 确定元数据中是否包含指定的键值对。
    /// </summary>
    /// <param name="item">要查找的键值对。</param>
    /// <returns>若键存在且值与 <paramref name="item"/> 相等则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public bool Contains(KeyValuePair<string, object> item) => _dictionary.Contains(item);

    /// <summary>
    /// 确定元数据中是否包含指定键。
    /// </summary>
    /// <param name="key">要查找的元数据键。</param>
    /// <returns>若存在 <paramref name="key"/> 则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    /// <summary>
    /// 移除具有指定键的元数据项。
    /// </summary>
    /// <param name="key">要移除的元数据键。</param>
    /// <returns>若成功移除则为 <c>true</c>；键不存在时为 <c>false</c>。</returns>
    public bool Remove(string key) => _dictionary.Remove(key);

    /// <summary>
    /// 从指定数组索引开始，将元数据项复制到目标数组。
    /// </summary>
    /// <param name="items">接收元数据项的目标数组。</param>
    /// <param name="index">目标数组中开始复制的索引。</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 时抛出。</exception>
    /// <exception cref="ArgumentException">目标数组剩余空间不足时抛出。</exception>
    public void CopyTo(KeyValuePair<string, object>[] items, int index) => ((ICollection<KeyValuePair<string, object>>)_dictionary).CopyTo(items, index);

    /// <summary>
    /// 返回用于遍历元数据项的枚举器。
    /// </summary>
    /// <returns>元数据项的枚举器。</returns>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dictionary.GetEnumerator();

    /// <summary>
    /// 返回用于遍历元数据项的非泛型枚举器。
    /// </summary>
    /// <returns>可用于遍历元数据项的非泛型枚举器。</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}