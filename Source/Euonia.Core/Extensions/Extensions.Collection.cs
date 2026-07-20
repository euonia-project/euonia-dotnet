using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Nerosoft.Euonia.Collections;

public static partial class Extensions
{
	private static readonly Random _random = new();

	/// <summary>
	/// 对 <see cref="IEnumerable{T}"/> 的每个元素执行指定操作。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="action">对每个元素执行的委托。</param>
	/// <exception cref="NullReferenceException">当 <paramref name="source"/> 为 null 时抛出。</exception>
	/// <exception cref="ArgumentNullException">当 action 为 null 时抛出。</exception>
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		ArgumentAssert.ThrowIfNull(action, nameof(action));

		foreach (var value in source)
		{
			action(value);
		}
	}

	/// <summary>
	/// 确定字符串集合是否包含指定值。
	/// </summary>
	/// <param name="source">源集合。</param>
	/// <param name="value">要查找的值。</param>
	/// <param name="comparison">字符串比较类型。</param>
	/// <returns>如果包含指定值，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="NullReferenceException">当 <paramref name="source"/> 为 null 时抛出。</exception>
	public static bool Contains(this IEnumerable<string> source, string value, StringComparison comparison)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		return source.Any(t => t.Equals(value, comparison));
	}

	/// <summary>
	/// 确定集合是否为 null 或空。
	/// </summary>
	/// <param name="source">源集合。</param>
	/// <returns>如果集合为 null 或空，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public static bool IsNullOrEmpty(this IEnumerable source)
	{
		if (source == null)
		{
			return true;
		}

		return !source.GetEnumerator().MoveNext();
	}

	/// <summary>
	/// 确定序列是否为 null 或空。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">要查找值的序列。</param>
	/// <returns>如果序列为 null 或空，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
	{
		return source == null || !source.Any();
	}

	/// <summary>
	/// 确定指定集合是否等于另一个集合。
	/// </summary>
	/// <typeparam name="T">集合元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="dest">目标集合。</param>
	/// <returns>如果集合相等，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="NullReferenceException">当 <paramref name="source"/> 为 null 时抛出。</exception>
	/// <exception cref="ArgumentNullException">当 <paramref name="dest"/> 为 null 时抛出。</exception>
	public static bool Equals<T>(this IEnumerable<T> source, IEnumerable<T> dest) where T : IComparable
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		ArgumentAssert.ThrowIfNull(dest, nameof(dest));

		return dest.Count() == source.Count() && source.All(dest.Contains);
	}

	/// <summary>
	/// 使用指定分隔符连接集合元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="values">包含要连接的对象的集合。</param>
	/// <param name="separator">分隔符。</param>
	/// <returns>连接后的字符串。</returns>
	public static string Join<T>(this IEnumerable<T> values, string separator)
	{
		if (values == null)
		{
			throw new NullReferenceException();
		}

		return string.Join(separator, values);
	}

	/// <summary>
	/// 使用指定分隔符连接集合中从指定位置开始的指定数量的元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="values">包含要连接的对象的集合。</param>
	/// <param name="separator">分隔符。</param>
	/// <param name="startIndex">起始索引。</param>
	/// <param name="count">要连接的元素数量。</param>
	/// <returns>连接后的字符串。</returns>
	public static string Join<T>(this IEnumerable<T> values, string separator, int startIndex, int count)
	{
		if (values == null)
		{
			throw new NullReferenceException();
		}

		if (startIndex >= values.Count())
		{
			throw new IndexOutOfRangeException();
		}

		return values.Skip(startIndex).Take(count).Join(separator);
	}

	/// <summary>
	/// 将可分页集合转换为视图集合。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源可分页集合。</param>
	/// <returns>视图集合。</returns>
	public static ViewCollection<T> ToView<T>(this PageableCollection<T> source) where T : class, new()
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		return new ViewCollection<T>(source.ToArray(), source.TotalCount);
	}

	/// <summary>
	/// 将 <see cref="IList{T}"/> 转换为可分页集合。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="totalCount">总记录数。</param>
	/// <param name="index">页码。</param>
	/// <param name="size">每页大小。</param>
	/// <returns>包含 <paramref name="source"/> 所有元素的新可分页集合。</returns>
	public static PageableCollection<T> Paginate<T>(this IList<T> source, long totalCount, int index, int size)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		return new PageableCollection<T>(source) { TotalCount = totalCount, PageNumber = index, PageSize = size };
	}

	/// <summary>
	/// 将现有可分页集合转换为另一个可分页集合。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源可分页集合。</param>
	/// <param name="index">页码。</param>
	/// <param name="size">每页大小。</param>
	/// <returns>包含 <paramref name="source"/> 所有元素的新可分页集合。</returns>
	public static PageableCollection<T> Convert<T>(this PageableCollection<T> source, int index, int size)
	{
		if (source == null)
		{
			throw new NullReferenceException(nameof(source));
		}

		return new PageableCollection<T>(source) { TotalCount = source.TotalCount, PageNumber = index, PageSize = size };
	}

	/// <summary>
	/// 随机打乱集合中的元素顺序。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="enumerable">要打乱的集合。</param>
	/// <returns>打乱顺序后的集合。</returns>
	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
	{
		var buffer = enumerable.ToList();

		for (var i = 0; i < buffer.Count; i++)
		{
			var j = _random.Next(i, buffer.Count);

			yield return buffer[j];

			buffer[j] = buffer[i];
		}
	}

	/// <summary>
	/// 将集合转换为 <see cref="ObservableCollection{T}"/>。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <returns>包含 <paramref name="source"/> 所有元素的 <see cref="ObservableCollection{T}"/>。</returns>
	public static ObservableCollection<T> ToObservable<T>(this IEnumerable<T> source)
	{
		var collection = new ObservableCollection<T>(source);
		return collection;
	}

	/// <summary>
	/// 将源集合具体化为 <see cref="IReadOnlyCollection{T}"/>。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <returns>包含 <paramref name="source"/> 所有元素的 <see cref="IReadOnlyCollection{T}"/>。</returns>
	public static IReadOnlyCollection<T> Reify<T>(this IEnumerable<T> source)
	{
		return source switch
		{
			null => throw new NullReferenceException(),
			IReadOnlyCollection<T> result => result,
			ICollection<T> collection => new CollectionWrapper<T>(collection),
			ICollection nonGenericCollection => new NonGenericCollectionWrapper<T>(nonGenericCollection),
			_ => new List<T>(source)
		};
	}

	/// <summary>
	/// 在列表的指定索引处批量插入元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="index">插入的起始索引。</param>
	/// <param name="items">要插入的元素集合。</param>
	public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			source.Insert(index++, item);
		}
	}

	/// <summary>
	/// 查找列表中匹配指定条件的第一个元素的索引。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <returns>匹配条件的第一个元素的索引，如果未找到则返回 -1。</returns>
	public static int FindIndex<T>(this IList<T> source, Predicate<T> selector)
	{
		for (var i = 0; i < source.Count; ++i)
		{
			if (selector(source[i]))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// 将元素添加到列表开头。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="item">要添加的元素。</param>
	public static void AddFirst<T>(this IList<T> source, T item)
	{
		source.Insert(0, item);
	}

	/// <summary>
	/// 将元素添加到列表末尾。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="item">要添加的元素。</param>
	public static void AddLast<T>(this IList<T> source, T item)
	{
		source.Insert(source.Count, item);
	}

	/// <summary>
	/// 在指定元素之后插入新元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="existingItem">指定的现有元素。</param>
	/// <param name="item">要插入的新元素。</param>
	public static void InsertAfter<T>(this IList<T> source, T existingItem, T item)
	{
		var index = source.IndexOf(existingItem);
		if (index < 0)
		{
			source.AddFirst(item);
			return;
		}

		source.Insert(index + 1, item);
	}

	/// <summary>
	/// 在匹配条件的第一个元素之后插入新元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="item">要插入的新元素。</param>
	public static void InsertAfter<T>(this IList<T> source, Predicate<T> selector, T item)
	{
		var index = source.FindIndex(selector);
		if (index < 0)
		{
			source.AddFirst(item);
			return;
		}

		source.Insert(index + 1, item);
	}

	/// <summary>
	/// 在指定元素之前插入新元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="existingItem">指定的现有元素。</param>
	/// <param name="item">要插入的新元素。</param>
	public static void InsertBefore<T>(this IList<T> source, T existingItem, T item)
	{
		var index = source.IndexOf(existingItem);
		if (index < 0)
		{
			source.AddLast(item);
			return;
		}

		source.Insert(index, item);
	}

	/// <summary>
	/// 在匹配条件的第一个元素之前插入新元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="item">要插入的新元素。</param>
	public static void InsertBefore<T>(this IList<T> source, Predicate<T> selector, T item)
	{
		var index = source.FindIndex(selector);
		if (index < 0)
		{
			source.AddLast(item);
			return;
		}

		source.Insert(index, item);
	}

	/// <summary>
	/// 替换列表中所有匹配条件的元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="item">要替换的元素。</param>
	public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, T item)
	{
		for (var i = 0; i < source.Count; i++)
		{
			if (selector(source[i]))
			{
				source[i] = item;
			}
		}
	}

	/// <summary>
	/// 使用工厂方法替换列表中所有匹配条件的元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="itemFactory">用于生成新元素的工厂方法。</param>
	public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
	{
		for (var i = 0; i < source.Count; i++)
		{
			var item = source[i];
			if (selector(item))
			{
				source[i] = itemFactory(item);
			}
		}
	}

	/// <summary>
	/// 替换列表中第一个匹配条件的元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="item">要替换的元素。</param>
	public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, T item)
	{
		for (var i = 0; i < source.Count; i++)
		{
			if (selector(source[i]))
			{
				source[i] = item;
				return;
			}
		}
	}

	/// <summary>
	/// 使用工厂方法替换列表中第一个匹配条件的元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="itemFactory">用于生成新元素的工厂方法。</param>
	public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
	{
		for (var i = 0; i < source.Count; i++)
		{
			var item = source[i];
			if (!selector(item))
			{
				continue;
			}

			source[i] = itemFactory(item);
			return;
		}
	}

	/// <summary>
	/// 替换列表中与指定元素相等的第一个元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="item">要替换的元素。</param>
	/// <param name="replaceWith">用于替换的新元素。</param>
	public static void ReplaceOne<T>(this IList<T> source, T item, T replaceWith)
	{
		for (var i = 0; i < source.Count; i++)
		{
			if (Comparer<T>.Default.Compare(source[i], item) != 0)
			{
				continue;
			}

			source[i] = replaceWith;
			return;
		}
	}

	/// <summary>
	/// 将匹配条件的元素移动到目标索引位置。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="targetIndex">目标索引位置。</param>
	public static void MoveItem<T>(this List<T> source, Predicate<T> selector, int targetIndex)
	{
		if (!targetIndex.IsBetween(0, source.Count - 1))
		{
			throw new IndexOutOfRangeException("targetIndex should be between 0 and " + (source.Count - 1));
		}

		var currentIndex = source.FindIndex(0, selector);
		if (currentIndex == targetIndex)
		{
			return;
		}

		var item = source[currentIndex];
		source.RemoveAt(currentIndex);
		source.Insert(targetIndex, item);
	}

	/// <summary>
	/// 从列表中获取匹配条件的元素，如果不存在则使用工厂方法创建并添加。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源列表。</param>
	/// <param name="selector">用于匹配元素的条件。</param>
	/// <param name="factory">用于创建新元素的工厂方法。</param>
	public static T GetOrAdd<T>([NotNull] this IList<T> source, Func<T, bool> selector, Func<T> factory)
	{
		Check.EnsureNotNull(source, nameof(source));

		var item = source.FirstOrDefault(selector);

		if (item == null)
		{
			item = factory();
			source.Add(item);
		}

		return item;
	}

	/// <summary>
	/// 使用拓扑排序对列表进行排序，考虑元素之间的依赖关系。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">要排序的对象列表。</param>
	/// <param name="getDependencies">解析依赖关系的函数。</param>
	/// <param name="comparer">依赖关系的相等比较器。</param>
	/// <returns>按依赖关系排序的新列表。如果 A 依赖于 B，则 B 在结果列表中排在 A 之前。</returns>
	public static List<T> SortByDependencies<T>(
		this IEnumerable<T> source,
		Func<T, IEnumerable<T>> getDependencies,
		IEqualityComparer<T> comparer = null)
	{
		var sorted = new List<T>();
		var visited = new Dictionary<T, bool>(comparer);

		foreach (var item in source)
		{
			SortByDependenciesVisit(item, getDependencies, sorted, visited);
		}

		return sorted;
	}

	/// <summary>
	/// 拓扑排序的递归访问方法。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="item">当前访问的元素。</param>
	/// <param name="getDependencies">解析依赖关系的函数。</param>
	/// <param name="sorted">已排序的元素列表。</param>
	/// <param name="visited">访问状态字典。</param>
	private static void SortByDependenciesVisit<T>(T item, Func<T, IEnumerable<T>> getDependencies, IList<T> sorted, Dictionary<T, bool> visited)
	{
		var alreadyVisited = visited.TryGetValue(item, out var inProcess);

		if (alreadyVisited)
		{
			if (inProcess)
			{
				throw new ArgumentException("Cyclic dependency found! Item: " + item);
			}
		}
		else
		{
			visited[item] = true;

			var dependencies = getDependencies(item);
			if (dependencies != null)
			{
				foreach (var dependency in dependencies)
				{
					SortByDependenciesVisit(dependency, getDependencies, sorted, visited);
				}
			}

			visited[item] = false;
			sorted.Add(item);
		}
	}

	/// <summary>
	/// 使用指定分隔符连接字符串集合。这是 string.Join(...) 的快捷方式。
	/// </summary>
	/// <param name="source">要连接的字符串集合。</param>
	/// <param name="separator">分隔符。</param>
	public static string JoinAsString(this IEnumerable<string> source, string separator)
	{
		return string.Join(separator, source);
	}

	/// <summary>
	/// 使用指定分隔符连接集合元素。这是 string.Join(...) 的快捷方式。
	/// </summary>
	/// <param name="source">要连接的集合。</param>
	/// <param name="separator">分隔符。</param>
	public static string JoinAsString<T>(this IEnumerable<T> source, string separator)
	{
		return string.Join(separator, source);
	}

	/// <summary>
	/// 在给定条件为 true 时按谓词筛选序列。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源序列。</param>
	/// <param name="condition">条件。</param>
	/// <param name="predicate">用于筛选的谓词。</param>
	public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
	{
		return condition
			? source.Where(predicate)
			: source;
	}

	/// <summary>
	/// 在给定条件为 true 时按带索引的谓词筛选序列。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源序列。</param>
	/// <param name="condition">条件。</param>
	/// <param name="predicate">用于筛选的带索引的谓词。</param>
	public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, int, bool> predicate)
	{
		return condition
			? source.Where(predicate)
			: source;
	}

	/// <summary>
	/// 尝试从字典中获取值。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <param name="value">输出参数，用于存储获取到的值。</param>
	/// <returns>如果成功获取到值，则返回 true；否则返回 false。</returns>
	internal static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T value)
	{
		if (dictionary.TryGetValue(key, out var valueObj) && valueObj is T result)
		{
			value = result;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// 从字典中获取指定键的值，如果找不到则返回默认值。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回默认值。</returns>
	public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.GetValueOrDefault(key);
	}

	/// <summary>
	/// 从字典中获取指定键的值，如果找不到则返回默认值。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回默认值。</returns>
	public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.TryGetValue(key, out var obj) ? obj : default;
	}

	/// <summary>
	/// 从只读字典中获取指定键的值，如果找不到则返回默认值。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回默认值。</returns>
	public static TValue GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.GetValueOrDefault(key);
	}

	/// <summary>
	/// 从并发字典中获取指定键的值，如果找不到则返回默认值。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回默认值。</returns>
	public static TValue GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.GetValueOrDefault(key);
	}

	/// <summary>
	/// 从字典中获取指定键的值，如果找不到则使用工厂方法创建并添加。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <param name="factory">用于创建新值的工厂方法。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回工厂方法创建的新值。</returns>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
	{
		if (dictionary.TryGetValue(key, out var obj))
		{
			return obj;
		}

		return dictionary[key] = factory(key);
	}

	/// <summary>
	/// 从字典中获取指定键的值，如果找不到则使用工厂方法创建并添加。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">键。</param>
	/// <param name="factory">用于创建新值的工厂方法。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回工厂方法创建的新值。</returns>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
	{
		return dictionary.GetOrAdd(key, _ => factory());
	}

	/// <summary>
	/// 如果集合中尚未包含指定元素，则将其添加。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="item">要添加的元素。</param>
	/// <returns>如果元素已存在于集合中，则返回 false；否则返回 true。</returns>
	public static bool AddIfNotContains<T>([NotNull] this ICollection<T> source, T item)
	{
		Check.EnsureNotNull(source, nameof(source));

		if (source.Contains(item))
		{
			return false;
		}

		source.Add(item);
		return true;
	}

	/// <summary>
	/// 将集合中尚未包含的元素批量添加。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="items">要添加的元素集合。</param>
	/// <returns>返回实际添加到集合中的元素。</returns>
	public static IEnumerable<T> AddIfNotContains<T>([NotNull] this ICollection<T> source, IEnumerable<T> items)
	{
		Check.EnsureNotNull(source, nameof(source));

		var addedItems = new List<T>();

		foreach (var item in items)
		{
			if (source.Contains(item))
			{
				continue;
			}

			source.Add(item);
			addedItems.Add(item);
		}

		return addedItems;
	}

	/// <summary>
	/// 如果集合中不包含匹配指定条件的元素，则使用工厂方法创建并添加。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="predicate">用于检查元素是否存在的条件。</param>
	/// <param name="itemFactory">用于创建新元素的工厂方法。</param>
	/// <returns>如果元素已存在于集合中，则返回 false；否则返回 true。</returns>
	public static bool AddIfNotContains<T>([NotNull] this ICollection<T> source, [NotNull] Func<T, bool> predicate, [NotNull] Func<T> itemFactory)
	{
		Check.EnsureNotNull(source, nameof(source));
		Check.EnsureNotNull(predicate, nameof(predicate));
		Check.EnsureNotNull(itemFactory, nameof(itemFactory));

		if (source.Any(predicate))
		{
			return false;
		}

		source.Add(itemFactory());
		return true;
	}

	/// <summary>
	/// 从集合中移除所有满足指定条件的元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="predicate">用于筛选要移除的元素的条件。</param>
	/// <returns>返回实际从集合中移除的元素。</returns>
	public static IList<T> RemoveAll<T>([NotNull] this ICollection<T> source, Func<T, bool> predicate)
	{
		var items = source.Where(predicate).ToList();

		foreach (var item in items)
		{
			source.Remove(item);
		}

		return items;
	}

	/// <summary>
	/// 从集合中移除指定的所有元素。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="source">源集合。</param>
	/// <param name="items">要移除的元素集合。</param>
	public static void RemoveAll<T>([NotNull] this ICollection<T> source, IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			source.Remove(item);
		}
	}

	/// <summary>
	/// 设置字典中指定键的值并返回字典本身，支持链式调用。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">要设置的键。</param>
	/// <param name="value">要设置的值。</param>
	/// <returns>返回字典本身，以支持链式调用。</returns>
	public static IDictionary<TKey, TValue> Set<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
	{
		dictionary[key] = value;
		return dictionary;
	}

	/// <summary>
	/// 尝试从字典中获取值并转换为指定类型后执行回调。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <typeparam name="TRef">要转换为的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <param name="func">获取到值后要执行的回调函数。</param>
	public static void TryGetValue<TKey, TValue, TRef>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TRef> func)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return;
		}

		var refValue = (TRef)System.Convert.ChangeType(value, typeof(TRef));
		func(refValue);
	}

	/// <summary>
	/// 尝试从字典中获取 object 类型的值并执行回调。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TRef">要转换为的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <param name="func">获取到值后要执行的回调函数。</param>
	public static void TryGetValue<TKey, TRef>(this IDictionary<TKey, object> dictionary, TKey key, Action<TRef> func)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return;
		}

		var refValue = (TRef)value;
		func(refValue);
	}

	/// <summary>
	/// 尝试从字典中获取值并执行回调。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <param name="func">获取到值后要执行的回调函数。</param>
	public static void TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> func)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return;
		}

		func(value);
	}

	/// <summary>
	/// 使用指定的字符串比较方式从字典中获取值。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="dictionary">字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <param name="comparison">字符串比较方式。</param>
	/// <returns>返回获取到的值，如果未找到则返回默认值。</returns>
	public static TValue GetValue<TValue>(this IDictionary<string, TValue> dictionary, string key, StringComparison comparison)
	{
		var item = dictionary.FirstOrDefault(t => t.Key.Equals(key, comparison));
		return item.Value;
	}

	/// <summary>
	/// 尝试从字典中获取指定键的值。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="source">源字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回默认值。</returns>
	public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		return source.TryGetValue(key, out var value) ? value : default;
	}

	/// <summary>
	/// 尝试从字典中获取指定键的值，如果键不存在则返回默认值。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="source">源字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <param name="defaultValue">键不存在时返回的默认值。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回 <paramref name="defaultValue"/>。</returns>
	public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue defaultValue)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		return source.TryGetValue(key, out var value) ? value : defaultValue;
	}

	/// <summary>
	/// 尝试获取指定键的值，如果不存在则设置默认值并返回。
	/// </summary>
	/// <typeparam name="TKey">键的类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="source">源字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回默认值。</returns>
	public static TValue TryGetOrSetValue<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		if (source.TryGetValue(key, out var value))
		{
			return value;
		}

		value = default;
		source.Add(key, value);
		return source[key];
	}

	/// <summary>
	/// 使用指定的字符串比较方式从字典中尝试获取值。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="source">源字典。</param>
	/// <param name="key">要获取的键。</param>
	/// <param name="defaultValue">键不存在时返回的默认值。</param>
	/// <param name="comparison">字符串比较方式。</param>
	/// <returns>如果找到键，则返回对应的值；否则返回 <paramref name="defaultValue"/>。</returns>
	public static TValue TryGetValue<TValue>(this IDictionary<string, TValue> source, string key, TValue defaultValue, StringComparison comparison)
	{
		if (source == null)
		{
			throw new NullReferenceException();
		}

		return source.Keys.Contains(key, comparison) ? source.FirstOrDefault(t => t.Key.Equals(key, comparison)).Value : defaultValue;
	}

	/// <summary>
	/// 获取指定元素在集合中的索引。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="enumerable">源集合。</param>
	/// <param name="item">要查找的元素。</param>
	/// <returns>如果找到元素，则返回其索引；否则返回 -1。</returns>
	public static int IndexOf<T>(this IEnumerable<T> enumerable, T item)
	{
		if (enumerable == null)
			throw new ArgumentNullException(nameof(enumerable));

		var i = 0;
		foreach (var element in enumerable)
		{
			if (Equals(element, item))
			{
				return i;
			}

			i++;
		}

		return -1;
	}

	/// <summary>
	/// 获取匹配条件的第一个元素在集合中的索引。
	/// </summary>
	/// <typeparam name="T">元素类型。</typeparam>
	/// <param name="enumerable">源集合。</param>
	/// <param name="predicate">用于匹配元素的条件。</param>
	/// <returns>如果找到匹配条件的元素，则返回其索引；否则返回 -1。</returns>
	public static int IndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
	{
		var i = 0;
		foreach (var element in enumerable)
		{
			if (predicate(element))
			{
				return i;
			}

			i++;
		}

		return -1;
	}
}