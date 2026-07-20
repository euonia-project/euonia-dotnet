using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 可观察分组的可观察列表。
/// </summary>
/// <typeparam name="TKey">分组键的类型。</typeparam>
/// <typeparam name="TValue">集合中元素的类型。</typeparam>
public sealed class ObservableGroupedCollection<TKey, TValue> : ObservableCollection<ObservableGroup<TKey, TValue>>
{
    /// <summary>
    /// 初始化 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 类的新实例。
    /// </summary>
    public ObservableGroupedCollection()
    {
    }

    /// <summary>
    /// 初始化 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="collection">要添加到分组集合中的初始数据。</param>
    public ObservableGroupedCollection(IEnumerable<IGrouping<TKey, TValue>> collection)
        : base(collection.Select(c => new ObservableGroup<TKey, TValue>(c)))
    {
    }

    /// <summary>
    /// 尝试获取底层的 <see cref="List{T}"/> 实例（如果存在）。
    /// </summary>
    /// <param name="list">结果 <see cref="List{T}"/>（如果正在使用的话）。</param>
    /// <returns>是否找到了 <see cref="List{T}"/> 实例。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetList(out List<ObservableGroup<TKey, TValue>> list)
    {
        list = Items as List<ObservableGroup<TKey, TValue>>;

        return list is not null;
    }
}
