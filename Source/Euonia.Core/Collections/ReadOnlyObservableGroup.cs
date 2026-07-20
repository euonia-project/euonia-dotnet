using System.Collections.ObjectModel;

namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 只读的可观察分组。它将 <see cref="Key"/> 与一个 <see cref="ReadOnlyObservableCollection{T}"/> 关联起来。
/// </summary>
/// <typeparam name="TKey">分组键的类型。</typeparam>
/// <typeparam name="TValue">集合中元素的类型。</typeparam>
public sealed class ReadOnlyObservableGroup<TKey, TValue> : ReadOnlyObservableCollection<TValue>, IGrouping<TKey, TValue>, IReadOnlyObservableGroup
{
    /// <summary>
    /// 初始化 <see cref="ReadOnlyObservableGroup{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="key">分组的键。</param>
    /// <param name="collection">要添加到该分组的元素集合。</param>
    public ReadOnlyObservableGroup(TKey key, ObservableCollection<TValue> collection)
        : base(collection)
    {
        Key = key;
    }

    /// <summary>
    /// 初始化 <see cref="ReadOnlyObservableGroup{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="group">要包装的 <see cref="ObservableGroup{TKey, TValue}"/>。</param>
    public ReadOnlyObservableGroup(ObservableGroup<TKey, TValue> group)
        : base(group)
    {
        Key = group.Key;
    }

    /// <summary>
    /// 初始化 <see cref="ReadOnlyObservableGroup{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="key">分组的键。</param>
    /// <param name="collection">要添加到该分组的元素集合。</param>
    public ReadOnlyObservableGroup(TKey key, IEnumerable<TValue> collection)
        : base(new ObservableCollection<TValue>(collection))
    {
        Key = key;
    }

    /// <inheritdoc/>
    public TKey Key { get; }

    /// <inheritdoc/>
    object IReadOnlyObservableGroup.Key => Key;
}
