using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 可观察的分组集合。
/// 它将 <see cref="Key"/> 与一个 <see cref="ObservableCollection{T}"/> 关联起来。
/// </summary>
/// <typeparam name="TKey">分组键的类型。</typeparam>
/// <typeparam name="TValue">集合中元素的类型。</typeparam>
[DebuggerDisplay("Key = {Key}, Count = {Count}")]
public sealed class ObservableGroup<TKey, TValue> : ObservableCollection<TValue>, IGrouping<TKey, TValue>, IReadOnlyObservableGroup
{
    /// <summary>
    /// 初始化 <see cref="ObservableGroup{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="key">分组的键。</param>
    public ObservableGroup(TKey key)
    {
        Key = key;
    }

    /// <summary>
    /// 初始化 <see cref="ObservableGroup{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="grouping">用于填充该分组的 <see cref="IGrouping{TKey, TValue}"/>。</param>
    public ObservableGroup(IGrouping<TKey, TValue> grouping)
        : base(grouping)
    {
        Key = grouping.Key;
    }

    /// <summary>
    /// 初始化 <see cref="ObservableGroup{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="key">分组的键。</param>
    /// <param name="collection">要添加到该分组的初始数据集合。</param>
    public ObservableGroup(TKey key, IEnumerable<TValue> collection)
        : base(collection)
    {
        Key = key;
    }

    /// <summary>
    /// 获取分组的键。
    /// </summary>
    public TKey Key { get; }

    /// <inheritdoc/>
    object IReadOnlyObservableGroup.Key => Key;
}
