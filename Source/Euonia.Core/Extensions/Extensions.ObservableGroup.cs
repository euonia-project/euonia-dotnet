using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Nerosoft.Euonia.Collections;

public static partial class Extensions
{
	/// <summary>
	/// 返回具有指定 <paramref name="key"/> 键的第一个分组。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey,TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <returns>匹配 <paramref name="key"/> 的第一个分组。</returns>
	/// <exception cref="InvalidOperationException">目标分组不存在。</exception>
	[Pure]
	public static ObservableGroup<TKey, TValue> First<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key)
	{
		var group = source.FirstOrDefault(key);

		if (group is null)
		{
			ThrowArgumentExceptionForKeyNotFound();
		}

		return group!;
	}

	/// <summary>
	/// 返回具有指定 <paramref name="key"/> 键的第一个分组，如果未找到则返回 null。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <returns>匹配 <paramref name="key"/> 的第一个分组，如果未找到则为 null。</returns>
	[Pure]
	public static ObservableGroup<TKey, TValue> FirstOrDefault<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key)
	{
		if (source.TryGetList(out var list))
		{
			foreach (var group in list!)
			{
				if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
				{
					return group;
				}
			}

			return null;
		}

		return FirstOrDefaultWithLinq(source, key);
	}

	/// <summary>
	/// <see cref="First{TKey,TValue}"/> 的慢速路径。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <returns>匹配 <paramref name="key"/> 的第一个分组，如果未找到则为 null。</returns>
	[Pure]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static ObservableGroup<TKey, TValue> FirstOrDefaultWithLinq<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key)
		=> source.FirstOrDefault(group => EqualityComparer<TKey>.Default.Equals(group.Key, key));

	/// <summary>
	/// 返回具有指定 <paramref name="key"/> 键的第一个分组中位于 <paramref name="index"/> 位置的元素。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="index">目标分组中元素的索引。</param>
	/// <returns>指定位置的元素。</returns>
	/// <exception cref="InvalidOperationException">目标分组不存在。</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于零或大于分组元素数量。</exception>
	[Pure]
	public static TValue ElementAt<TKey, TValue>(
		this ObservableGroupedCollection<TKey, TValue> source,
		TKey key,
		int index)
		=> source.First(key)[index];

	/// <summary>
	/// 返回具有指定 <paramref name="key"/> 键的第一个分组中位于 <paramref name="index"/> 位置的元素，如果不存在则返回默认值。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="index">目标分组中元素的索引。</param>
	/// <returns>指定位置的元素，如果不存在则为默认值。</returns>
	[Pure]
	public static TValue ElementAtOrDefault<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, int index)
	{
		var group = source.FirstOrDefault(key);

		if (group is null ||
		    (uint)index >= (uint)group.Count)
		{
			return default!;
		}

		return group[index];
	}

	/// <summary>
	/// 向目标 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 中添加一个键值对分组。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要添加的分组键。</param>
	/// <param name="value">要添加的分组值。</param>
	/// <returns>新添加的分组。</returns>
	public static ObservableGroup<TKey, TValue> AddGroup<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue value)
		=> AddGroup(source, key, [value]);

	/// <summary>
	/// 向目标 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 中添加一个键-集合分组。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要添加的分组键。</param>
	/// <param name="collection">要添加的分组集合。</param>
	/// <returns>新添加的分组。</returns>
	public static ObservableGroup<TKey, TValue> AddGroup<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, params TValue[] collection) 
		=> source.AddGroup(key, (IEnumerable<TValue>)collection);

	/// <summary>
	/// 向目标 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 中添加一个键-集合分组。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要添加的分组键。</param>
	/// <param name="collection">要添加的分组集合。</param>
	/// <returns>新添加的分组。</returns>
	public static ObservableGroup<TKey, TValue> AddGroup<TKey, TValue>(
		this ObservableGroupedCollection<TKey, TValue> source,
		TKey key,
		IEnumerable<TValue> collection)
	{
		var group = new ObservableGroup<TKey, TValue>(key, collection);
		source.Add(group);

		return group;
	}

	/// <summary>
	/// 将 <paramref name="item"/> 添加到具有指定 <paramref name="key"/> 键的第一个分组中。如果分组不存在，则会自动创建。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="item">要添加的元素。</param>
	/// <returns>添加元素的分组。</returns>
	public static ObservableGroup<TKey, TValue> AddItem<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item)
	{
		var group = source.FirstOrDefault(key);

		if (group is null)
		{
			group = new ObservableGroup<TKey, TValue>(key);
			source.Add(group);
		}

		group.Add(item);

		return group;
	}

	/// <summary>
	/// 在具有指定 <paramref name="key"/> 键的第一个分组的 <paramref name="index"/> 位置插入 <paramref name="item"/>。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="index">要插入的索引。</param>
	/// <param name="item">要插入的元素。</param>
	/// <returns>插入元素的分组。</returns>
	public static ObservableGroup<TKey, TValue> InsertItem<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, int index, TValue item)
	{
		var existingGroup = source.First(key);
		existingGroup.Insert(index, item);

		return existingGroup;
	}

	/// <summary>
	/// 替换具有指定 <paramref name="key"/> 键的第一个分组中 <paramref name="index"/> 位置的元素为 <paramref name="item"/>。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="index">要替换的索引。</param>
	/// <param name="item">要替换的元素。</param>
	/// <returns>替换元素的分组。</returns>
	public static ObservableGroup<TKey, TValue> SetItem<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, int index, TValue item)
	{
		var existingGroup = source.First(key);
		existingGroup[index] = item;

		return existingGroup;
	}

	/// <summary>
	/// 从 <paramref name="source"/> 分组集合中移除第一个具有指定 <paramref name="key"/> 键的分组。如果分组不存在，则不执行任何操作。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要移除的分组键。</param>
	/// <returns>如果成功移除分组，则为 true；否则为 false。</returns>
	public static bool RemoveGroup<TKey, TValue>(
		this ObservableGroupedCollection<TKey, TValue> source,
		TKey key)
	{
		if (source.TryGetList(out var list))
		{
			var index = 0;
			foreach (var group in list!)
			{
				if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
				{
					source.RemoveAt(index);

					return true;
				}

				index++;
			}
		}
		else
		{
			return RemoveGroupWithLinq(source, key);
		}

		return false;
	}

	/// <summary>
	/// <see cref="RemoveGroup{TKey,TValue}"/> 的慢速路径。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要移除的分组键。</param>
	/// <returns>如果成功移除分组，则为 true；否则为 false。</returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool RemoveGroupWithLinq<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key)
	{
		var index = 0;
		foreach (var group in source)
		{
			if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
			{
				source.RemoveAt(index);
				return true;
			}

			index++;
		}

		return false;
	}

	/// <summary>
	/// 从 <paramref name="source"/> 分组集合中移除具有指定 <paramref name="key"/> 键的第一个分组中的第一个 <paramref name="item"/>。如果分组或元素不存在，则不执行任何操作。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="item">要移除的元素。</param>
	/// <param name="removeGroupIfEmpty">如果分组为空，是否移除该分组。</param>
	public static void RemoveItem<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item, bool removeGroupIfEmpty = true)
	{
		if (source.TryGetList(out var list))
		{
			var index = 0;
			foreach (var group in list!)
			{
				if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
				{
					if (group.Remove(item) &&
					    removeGroupIfEmpty &&
					    group.Count == 0)
					{
						source.RemoveAt(index);
					}

					return;
				}

				index++;
			}
		}
		else
		{
			RemoveItemWithLinq(source, key, item, removeGroupIfEmpty);
		}
	}

	/// <summary>
	/// <see cref="RemoveItem{TKey,TValue}"/> 的慢速路径。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="item">要移除的元素。</param>
	/// <param name="removeGroupIfEmpty">如果分组为空，是否移除该分组。</param>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RemoveItemWithLinq<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item, bool removeGroupIfEmpty)
	{
		var index = 0;
		foreach (var group in source)
		{
			if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
			{
				if (group.Remove(item) && removeGroupIfEmpty && group.Count == 0)
				{
					source.RemoveAt(index);
				}

				return;
			}

			index++;
		}
	}

	/// <summary>
	/// 从 <paramref name="source"/> 分组集合中移除具有指定 <paramref name="key"/> 键的第一个分组中位于 <paramref name="index"/> 位置的元素。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="index">要移除的索引。</param>
	/// <param name="removeGroupIfEmpty">如果分组为空，是否移除该分组。</param>
	/// <remarks>
	/// 如果指定的分组不存在，则不执行任何操作。
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">当指定的索引超出范围时抛出。</exception>
	public static void RemoveItemAt<TKey, TValue>(this ObservableGroupedCollection<TKey, TValue> source, TKey key, int index, bool removeGroupIfEmpty = true)
	{
		if (source.TryGetList(out var list))
		{
			var groupIndex = 0;
			foreach (var group in list!)
			{
				if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
				{
					group.RemoveAt(index);

					if (removeGroupIfEmpty && group.Count == 0)
					{
						source.RemoveAt(groupIndex);
					}

					return;
				}

				groupIndex++;
			}
		}
		else
		{
			RemoveItemAtWithLinq(source, key, index, removeGroupIfEmpty);
		}
	}

	/// <summary>
	/// <see cref="RemoveItemAt{TKey,TValue}"/> 的慢速路径。
	/// </summary>
	/// <typeparam name="TKey">分组键的类型。</typeparam>
	/// <typeparam name="TValue">集合中元素的类型。</typeparam>
	/// <param name="source">源 <see cref="ObservableGroupedCollection{TKey, TValue}"/> 实例。</param>
	/// <param name="key">要查询的分组键。</param>
	/// <param name="index">要移除的索引。</param>
	/// <param name="removeGroupIfEmpty">如果分组为空，是否移除该分组。</param>
	/// <exception cref="ArgumentOutOfRangeException">当指定的索引超出范围时抛出。</exception>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RemoveItemAtWithLinq<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, int index, bool removeGroupIfEmpty)
	{
		var groupIndex = 0;
		foreach (var group in source)
		{
			if (EqualityComparer<TKey>.Default.Equals(group.Key, key))
			{
				if (index < 0 || index >= group.Count)
				{
					throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
				}

				group.RemoveAt(index);

				if (removeGroupIfEmpty && group.Count == 0)
				{
					source.RemoveAt(groupIndex);
				}

				return;
			}

			groupIndex++;
		}
	}

	/// <summary>
	/// 当找不到键时抛出一个新的 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <remarks>
	/// 该方法使用 <see cref="MethodImplOptions.NoInlining"/> 特性标记，以确保在调用时不会被内联，从而提供更准确的堆栈跟踪信息。
	/// </remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowArgumentExceptionForKeyNotFound()
	{
		throw new InvalidOperationException("The requested key was not present in the collection");
	}
}