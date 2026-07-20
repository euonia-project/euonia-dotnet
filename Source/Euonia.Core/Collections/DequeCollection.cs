namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 双端队列集合类。此类不能被继承。
/// 实现了 <see cref="IList{T}" />、<see cref="IReadOnlyList{T}" />、<see cref="IList" />
/// </summary>
/// <typeparam name="T"></typeparam>
/// <seealso cref="IList{T}" />
/// <seealso cref="IReadOnlyList{T}" />
/// <seealso cref="IList" />
public sealed class DequeCollection<T> : IList<T>, IReadOnlyList<T>, IList
{
	/// <summary>
	/// 默认容量。
	/// </summary>
	private const int DEFAULT_CAPACITY = 8;

	/// <summary>
	/// 存放视图的循环缓冲区。
	/// </summary>
	private T[] _buffer;

	/// <summary>
	/// 视图在 <see cref="_buffer" /> 中的起始偏移量。
	/// </summary>
	private int _offset;

	/// <summary>
	/// 使用指定容量初始化 <see cref="DequeCollection{T}" /> 类的新实例。
	/// </summary>
	/// <param name="capacity">初始容量，必须大于 <c>0</c>。</param>
	/// <exception cref="ArgumentOutOfRangeException">capacity - 容量不能为负数。</exception>
	public DequeCollection(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity), Resources.IDS_CAPACITY_MAY_NOT_BE_NEGATIVE);
		}

		_buffer = new T[capacity];
	}

	/// <summary>
	/// 使用指定集合中的元素初始化 <see cref="DequeCollection{T}" /> 类的新实例。
	/// </summary>
	/// <param name="collection">集合，不能为 <c>null</c>。</param>
	/// <exception cref="ArgumentNullException">collection</exception>
	public DequeCollection(IEnumerable<T> collection)
	{
		if (collection == null)
			throw new ArgumentNullException(nameof(collection));

		var source = collection.Reify();
		var count = source.Count;
		if (count > 0)
		{
			_buffer = new T[count];
			DoInsertRange(0, source);
		}
		else
		{
			_buffer = new T[DEFAULT_CAPACITY];
		}
	}

	/// <summary>
	/// 初始化 <see cref="DequeCollection{T}" /> 类的新实例。
	/// </summary>
	public DequeCollection()
		: this(DEFAULT_CAPACITY)
	{
	}

	#region GenericListImplementations

	/// <summary>
	/// 获取一个值，指示此列表是否为只读。此实现始终返回 <c>false</c>。
	/// </summary>
	/// <value>如果此实例为只读，则为 <c>true</c>；否则为 <c>false</c>。</value>
	bool ICollection<T>.IsReadOnly => false;

	/// <summary>
	/// 获取或设置指定索引处的元素。
	/// </summary>
	/// <param name="index">索引。</param>
	/// <returns>T。</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> 不是此列表中的有效索引。</exception>
	/// <exception cref="T:System.NotSupportedException">设置了此属性但列表为只读。</exception>
	public T this[int index]
	{
		get
		{
			CheckExistingIndexArgument(Count, index);
			return DoGetItem(index);
		}

		set
		{
			CheckExistingIndexArgument(Count, index);
			DoSetItem(index, value);
		}
	}

	/// <summary>
	/// 在指定索引处将元素插入此列表。
	/// </summary>
	/// <param name="index">应插入 <paramref name="item" /> 的从零开始的索引。</param>
	/// <param name="item">要插入此列表的对象。</param>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> 不是此列表中的有效索引。</exception>
	/// <exception cref="T:System.NotSupportedException">此列表为只读。</exception>
	public void Insert(int index, T item)
	{
		CheckNewIndexArgument(Count, index);
		DoInsert(index, item);
	}

	/// <summary>
	/// 移除指定索引处的元素。
	/// </summary>
	/// <param name="index">要移除的元素的从零开始的索引。</param>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> 不是此列表中的有效索引。</exception>
	/// <exception cref="T:System.NotSupportedException">此列表为只读。</exception>
	public void RemoveAt(int index)
	{
		CheckExistingIndexArgument(Count, index);
		DoRemoveAt(index);
	}

	/// <summary>
	/// 确定此列表中特定项的索引。
	/// </summary>
	/// <param name="item">要在此列表中定位的对象。</param>
	/// <returns>如果在此列表中找到 <paramref name="item" />，则为其索引；否则为 -1。</returns>
	public int IndexOf(T item)
	{
		var comparer = EqualityComparer<T>.Default;
		var ret = 0;
		foreach (var sourceItem in this)
		{
			if (comparer.Equals(item, sourceItem))
				return ret;
			++ret;
		}

		return -1;
	}

	/// <summary>
	/// 将元素添加到此列表的末尾。
	/// </summary>
	/// <param name="item">要添加到此列表的对象。</param>
	/// <exception cref="T:System.NotSupportedException">此列表为只读。</exception>
	void ICollection<T>.Add(T item)
	{
		DoInsert(Count, item);
	}

	/// <summary>
	/// 确定此列表是否包含特定值。
	/// </summary>
	/// <param name="item">要在此列表中定位的对象。</param>
	/// <returns>如果在此列表中找到 <paramref name="item" />，则为 true；否则为 false。</returns>
	bool ICollection<T>.Contains(T item)
	{
		var comparer = EqualityComparer<T>.Default;
		foreach (var entry in this)
		{
			if (comparer.Equals(item, entry))
				return true;
		}

		return false;
	}

	/// <summary>
	/// 将此列表的元素复制到 <see cref="T:System.Array" />，从特定的 <see cref="T:System.Array" /> 索引处开始。
	/// </summary>
	/// <param name="array">作为从此切片复制的元素的目标的一维 <see cref="T:System.Array" />。<see cref="T:System.Array" /> 必须具有从零开始的索引。</param>
	/// <param name="arrayIndex"><paramref name="array" /> 中从零开始的索引，从此处开始复制。</param>
	/// <exception cref="ArgumentNullException">array</exception>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> 为 null。</exception>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> 小于 0。</exception>
	/// <exception cref="T:System.ArgumentException"><paramref name="arrayIndex" /> 等于或大于 <paramref name="array" /> 的长度。
	/// -或-
	/// 源 <see cref="T:System.Collections.Generic.ICollection`1" /> 中的元素数量大于从 <paramref name="arrayIndex" /> 到目标 <paramref name="array" /> 末尾的可用空间。</exception>
	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}

		var count = Count;
		CheckRangeArguments(array.Length, arrayIndex, count);
		CopyToArray(array, arrayIndex);
	}

	/// <summary>
	/// 将双端队列元素复制到数组中。结果数组始终连续包含所有双端队列元素。
	/// </summary>
	/// <param name="array">目标数组。</param>
	/// <param name="arrayIndex">目标数组中开始写入的可选索引。</param>
	/// <exception cref="ArgumentNullException">array</exception>
	private void CopyToArray(Array array, int arrayIndex = 0)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		if (IsSplit)
		{
			// 当前缓冲区是分离的，因此必须分段复制
			var length = Capacity - _offset;
			Array.Copy(_buffer, _offset, array, arrayIndex, length);
			Array.Copy(_buffer, 0, array, arrayIndex + length, Count - length);
		}
		else
		{
			// 当前缓冲区是连续的
			Array.Copy(_buffer, _offset, array, arrayIndex, Count);
		}
	}

	/// <summary>
	/// 从此列表中移除特定对象的第一个匹配项。
	/// </summary>
	/// <param name="item">要从此列表中移除的对象。</param>
	/// <returns>如果 <paramref name="item" /> 已成功从此列表中移除，则为 true；否则为 false。如果在此列表中未找到 <paramref name="item" />，此方法也会返回 false。</returns>
	/// <exception cref="T:System.NotSupportedException">此列表为只读。</exception>
	public bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index == -1)
			return false;

		DoRemoveAt(index);
		return true;
	}

	/// <summary>
	/// 返回一个循环访问集合的枚举器。
	/// </summary>
	/// <returns>可用于循环访问集合的 <see cref="T:System.Collections.Generic.IEnumerator`1" />。</returns>
	public IEnumerator<T> GetEnumerator()
	{
		var count = Count;
		for (var i = 0; i != count; ++i)
		{
			yield return DoGetItem(i);
		}
	}

	/// <summary>
	/// 返回一个循环访问集合的枚举器。
	/// </summary>
	/// <returns>可用于循环访问集合的 <see cref="T:System.Collections.IEnumerator" /> 对象。</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	#region ObjectListImplementations

	/// <summary>
	/// 确定指定的值是否为类型 T。
	/// </summary>
	/// <param name="value">要检查的值。</param>
	/// <returns>如果指定的值是类型 T，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	private static bool IsT(object value)
	{
		if (value is T)
			return true;
		if (value != null)
			return false;
		return default(T) == null;
	}

	/// <summary>
	/// 添加指定的值。
	/// </summary>
	/// <param name="value">要添加的值。</param>
	/// <returns>新元素的索引。</returns>
	/// <exception cref="ArgumentNullException">value - 值不能为 null。</exception>
	/// <exception cref="ArgumentException">值的类型不正确。 - value</exception>
	int IList.Add(object value)
	{
		if (value == null && default(T) != null)
		{
			throw new ArgumentNullException(nameof(value), Resources.IDS_VALUE_CANNOT_BE_NULL);
		}

		if (!IsT(value))
		{
			throw new ArgumentException(Resources.IDS_VALUE_IS_OF_INCORRECT_TYPE, nameof(value));
		}

		AddToBack((T)value);
		return Count - 1;
	}

	/// <summary>
	/// 确定此实例是否包含指定的对象。
	/// </summary>
	/// <param name="value">要定位的值。</param>
	/// <returns>如果包含指定的值，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool IList.Contains(object value)
	{
		return IsT(value) && ((ICollection<T>)this).Contains((T)value);
	}

	/// <summary>
	/// 获取指定值的索引。
	/// </summary>
	/// <param name="value">要定位的值。</param>
	/// <returns>值的索引。</returns>
	int IList.IndexOf(object value)
	{
		return IsT(value) ? IndexOf((T)value) : -1;
	}

	/// <summary>
	/// 在指定索引处插入值。
	/// </summary>
	/// <param name="index">索引。</param>
	/// <param name="value">要插入的值。</param>
	/// <exception cref="ArgumentNullException">value - 值不能为 null。</exception>
	/// <exception cref="ArgumentException">值的类型不正确。 - value</exception>
	void IList.Insert(int index, object value)
	{
		if (value == null && default(T) != null)
		{
			throw new ArgumentNullException(nameof(value), Resources.IDS_VALUE_CANNOT_BE_NULL);
		}

		if (!IsT(value))
		{
			throw new ArgumentException(Resources.IDS_VALUE_IS_OF_INCORRECT_TYPE, nameof(value));
		}

		Insert(index, (T)value);
	}

	/// <summary>
	/// 获取一个值，指示此实例是否具有固定大小。
	/// </summary>
	/// <value>如果此实例具有固定大小，则为 <c>true</c>；否则为 <c>false</c>。</value>
	bool IList.IsFixedSize => false;

	/// <summary>
	/// 获取一个值，指示此实例是否为只读。
	/// </summary>
	/// <value>如果此实例为只读，则为 <c>true</c>；否则为 <c>false</c>。</value>
	bool IList.IsReadOnly => false;

	/// <summary>
	/// 移除指定的值。
	/// </summary>
	/// <param name="value">要移除的值。</param>
	void IList.Remove(object value)
	{
		if (IsT(value))
		{
			Remove((T)value);
		}
	}

	/// <summary>
	/// 获取或设置指定索引处的 <see cref="System.Object"/>。
	/// </summary>
	/// <param name="index">索引。</param>
	/// <returns>指定索引处的对象。</returns>
	/// <exception cref="ArgumentNullException">value - 值不能为 null。</exception>
	/// <exception cref="ArgumentException">值的类型不正确。 - value</exception>
	object IList.this[int index]
	{
		get => this[index];

		set
		{
			if (value == null && default(T) != null)
			{
				throw new ArgumentNullException(nameof(value), Resources.IDS_VALUE_CANNOT_BE_NULL);
			}

			if (!IsT(value))
			{
				throw new ArgumentException(Resources.IDS_VALUE_IS_OF_INCORRECT_TYPE, nameof(value));
			}

			this[index] = (T)value;
		}
	}

	/// <summary>
	/// 将元素复制到指定数组。
	/// </summary>
	/// <param name="array">目标数组。</param>
	/// <param name="index">起始索引。</param>
	/// <exception cref="ArgumentNullException">array - 目标数组不能为 null。</exception>
	/// <exception cref="ArgumentException">
	/// 目标数组类型不正确。 - array
	/// 或
	/// 目标数组必须是一维数组。 - array
	/// </exception>
	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array), Resources.IDS_DESTINATION_ARRAY_CANNOT_BE_NULL);
		}

		CheckRangeArguments(array.Length, index, Count);

		try
		{
			CopyToArray(array, index);
		}
		catch (ArrayTypeMismatchException ex)
		{
			throw new ArgumentException(Resources.IDS_DESTINATION_ARRAY_IS_OF_INCORRECT_TYPE, nameof(array), ex);
		}
		catch (RankException ex)
		{
			throw new ArgumentException(Resources.IDS_DESTINATION_ARRAY_MUST_BE_SINGLE_DIMENSIONAL, nameof(array), ex);
		}
	}

	/// <summary>
	/// 获取一个值，指示此实例是否已同步。
	/// </summary>
	/// <value>如果此实例已同步，则为 <c>true</c>；否则为 <c>false</c>。</value>
	bool ICollection.IsSynchronized => false;

	/// <summary>
	/// 获取同步根对象。
	/// </summary>
	/// <value>同步根对象。</value>
	object ICollection.SyncRoot => this;

	#endregion

	#region GenericListHelpers

	/// <summary>
	/// 检查 <paramref name="index" /> 参数是否指向给定长度源中的有效插入点。
	/// </summary>
	/// <param name="sourceLength">源的长度。此参数不检查有效性。</param>
	/// <param name="index">源中的索引。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> 不是源的有效插入点索引。</exception>
	private static void CheckNewIndexArgument(int sourceLength, int index)
	{
		if (index < 0 || index > sourceLength)
		{
			throw new ArgumentOutOfRangeException(nameof(index), string.Format(Resources.IDS_INVALID_NEW_INDEX_FOR_SOURCE_LENGTH, index, sourceLength));
		}
	}

	/// <summary>
	/// 检查 <paramref name="index" /> 参数是否指向给定长度源中的现有元素。
	/// </summary>
	/// <param name="sourceLength">源的长度。此参数不检查有效性。</param>
	/// <param name="index">源中的索引。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> 不是源中现有元素的有效索引。</exception>
	private static void CheckExistingIndexArgument(int sourceLength, int index)
	{
		if (index < 0 || index >= sourceLength)
		{
			throw new ArgumentOutOfRangeException(nameof(index), string.Format(Resources.IDS_INVALID_EXISTING_INDEX_FOR_SOURCE_LENGTH, index, sourceLength));
		}
	}

	/// <summary>
	/// 检查 <paramref name="offset" /> 和 <paramref name="count" /> 参数在应用于给定长度源时的有效性。允许 0 元素范围，包括源末尾的 0 元素范围。
	/// </summary>
	/// <param name="sourceLength">源的长度。此参数不检查有效性。</param>
	/// <param name="offset">范围起始位置在源中的索引。</param>
	/// <param name="count">范围内的元素数量。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> 或 <paramref name="count" /> 小于 0。</exception>
	/// <exception cref="ArgumentException">范围 [offset, offset + count) 不在 [0, sourceLength) 范围内。</exception>
	private static void CheckRangeArguments(int sourceLength, int offset, int count)
	{
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), string.Format(Resources.IDS_INVALID_OFFSET, offset));
		}

		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count), string.Format(Resources.IDS_INVALID_COUNT, count));
		}

		if (sourceLength - offset < count)
		{
			throw new ArgumentException(string.Format(Resources.IDS_INVALID_OFFSET_OR_COUNT_FOR_SOURCE_LENGTH, offset, count, sourceLength));
		}
	}

	#endregion

	/// <summary>
	/// 获取一个值，指示此实例是否为空。
	/// </summary>
	/// <value>如果此实例为空，则为 <c>true</c>；否则为 <c>false</c>。</value>
	private bool IsEmpty => Count == 0;

	/// <summary>
	/// 获取一个值，指示此实例是否已满。
	/// </summary>
	/// <value>如果此实例已满，则为 <c>true</c>；否则为 <c>false</c>。</value>
	private bool IsFull => Count == Capacity;

	/// <summary>
	/// 获取一个值，指示缓冲区是否"分离"（即视图的起始位置在 <see cref="_buffer" /> 中的索引晚于结束位置）。
	/// </summary>
	/// <value>如果此实例是分离的，则为 <c>true</c>；否则为 <c>false</c>。</value>
	// "(offset + Count) > Capacity" 的防溢出版本
	private bool IsSplit => _offset > (Capacity - Count);

	/// <summary>
	/// 获取或设置此双端队列的容量。此值必须始终大于零，且不能设置为小于 <see cref="Count" /> 的值。
	/// </summary>
	/// <value>容量。</value>
	/// <exception cref="ArgumentOutOfRangeException">value - 容量不能设置为小于 Count 的值</exception>
	/// <exception cref="InvalidOperationException"><c>Capacity</c> 不能设置为小于 <see cref="Count" /> 的值。</exception>
	public int Capacity
	{
		get => _buffer.Length;

		set
		{
			if (value < Count)
			{
				throw new ArgumentOutOfRangeException(nameof(value), Resources.IDS_CAPACITY_CANNOT_BE_SET_TO_A_VALUE_LESS_THAN_COUNT);
			}

			if (value == _buffer.Length)
				return;

			// 创建新缓冲区并复制现有范围
			var newBuffer = new T[value];
			CopyToArray(newBuffer);

			// 设置为使用新缓冲区
			_buffer = newBuffer;
			_offset = 0;
		}
	}

	/// <summary>
	/// 获取此双端队列中包含的元素数量。
	/// </summary>
	/// <value>元素数量。</value>
	public int Count { get; private set; }

	/// <summary>
	/// 将偏移量应用于 <paramref name="index" />，得到缓冲区索引。
	/// </summary>
	/// <param name="index">双端队列索引。</param>
	/// <returns>缓冲区索引。</returns>
	private int DequeIndexToBufferIndex(int index)
	{
		return (index + _offset) % Capacity;
	}

	/// <summary>
	/// 获取指定视图索引处的元素。
	/// </summary>
	/// <param name="index">要获取的元素的从零开始的视图索引。此索引保证有效。</param>
	/// <returns>指定索引处的元素。</returns>
	private T DoGetItem(int index)
	{
		return _buffer[DequeIndexToBufferIndex(index)];
	}

	/// <summary>
	/// 设置指定视图索引处的元素。
	/// </summary>
	/// <param name="index">要设置的元素的从零开始的视图索引。此索引保证有效。</param>
	/// <param name="item">要存储到列表的元素。</param>
	private void DoSetItem(int index, T item)
	{
		_buffer[DequeIndexToBufferIndex(index)] = item;
	}

	/// <summary>
	/// 在指定视图索引处插入元素。
	/// </summary>
	/// <param name="index">应插入元素的从零开始的视图索引。此索引保证有效。</param>
	/// <param name="item">要存储到列表的元素。</param>
	private void DoInsert(int index, T item)
	{
		EnsureCapacityForOneElement();

		if (index == 0)
		{
			DoAddToFront(item);
			return;
		}
		else if (index == Count)
		{
			DoAddToBack(item);
			return;
		}

		DoInsertRange(index, new[] { item });
	}

	/// <summary>
	/// 移除指定视图索引处的元素。
	/// </summary>
	/// <param name="index">要移除的元素的从零开始的视图索引。此索引保证有效。</param>
	private void DoRemoveAt(int index)
	{
		if (index == 0)
		{
			DoRemoveFromFront();
			return;
		}
		else if (index == Count - 1)
		{
			DoRemoveFromBack();
			return;
		}

		DoRemoveRange(index, 1);
	}

	/// <summary>
	/// 使用模-<see cref="Capacity" /> 算术将 <see cref="_offset" /> 增加 <paramref name="value" />。
	/// </summary>
	/// <param name="value">要增加 <see cref="_offset" /> 的值。不能为负数。</param>
	/// <returns>递增后的 <see cref="_offset" /> 值。</returns>
	private int PostIncrement(int value)
	{
		var ret = _offset;
		_offset += value;
		_offset %= Capacity;
		return ret;
	}

	/// <summary>
	/// 使用模-<see cref="Capacity" /> 算术将 <see cref="_offset" /> 减少 <paramref name="value" />。
	/// </summary>
	/// <param name="value">要减少 <see cref="_offset" /> 的值。不能为负数或大于 <see cref="Capacity" />。</param>
	/// <returns>递减前的 <see cref="_offset" /> 值。</returns>
	private int PreDecrement(int value)
	{
		_offset -= value;
		if (_offset < 0)
			_offset += Capacity;
		return _offset;
	}

	/// <summary>
	/// 将单个元素插入视图的后端。调用此方法时 <see cref="IsFull" /> 必须为 false。
	/// </summary>
	/// <param name="value">要插入的元素。</param>
	private void DoAddToBack(T value)
	{
		_buffer[DequeIndexToBufferIndex(Count)] = value;
		++Count;
	}

	/// <summary>
	/// 将单个元素插入视图的前端。调用此方法时 <see cref="IsFull" /> 必须为 false。
	/// </summary>
	/// <param name="value">要插入的元素。</param>
	private void DoAddToFront(T value)
	{
		_buffer[PreDecrement(1)] = value;
		++Count;
	}

	/// <summary>
	/// 移除并返回视图中的最后一个元素。调用此方法时 <see cref="IsEmpty" /> 必须为 false。
	/// </summary>
	/// <returns>原来的最后一个元素。</returns>
	private T DoRemoveFromBack()
	{
		var ret = _buffer[DequeIndexToBufferIndex(Count - 1)];
		--Count;
		return ret;
	}

	/// <summary>
	/// 移除并返回视图中的第一个元素。调用此方法时 <see cref="IsEmpty" /> 必须为 false。
	/// </summary>
	/// <returns>原来的第一个元素。</returns>
	private T DoRemoveFromFront()
	{
		--Count;
		return _buffer[PostIncrement(1)];
	}

	/// <summary>
	/// 将一系列元素插入视图。
	/// </summary>
	/// <param name="index">要插入元素的视图索引。</param>
	/// <param name="collection">要插入的元素。<c>collection.Count</c> 与 <see cref="Count" /> 之和必须小于或等于 <see cref="Capacity" />。</param>
	private void DoInsertRange(int index, IReadOnlyCollection<T> collection)
	{
		var collectionCount = collection.Count;
		// 在现有列表中腾出空间
		if (index < Count / 2)
		{
			// 插入到列表的前半部分

			// 将较低的元素向下移动：[0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
			// 这会清空前 "index" 个元素，将它们向下移动 "collectionCount" 个位置；
			//   旋转后，将在 "index" 处留下一个 "collectionCount" 大小的空隙。
			var copyCount = index;
			var writeIndex = Capacity - collectionCount;
			for (var j = 0; j != copyCount; ++j)
				_buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];

			// 旋转到新视图
			PreDecrement(collectionCount);
		}
		else
		{
			// 插入到列表的后半部分

			// 将较高的元素向上移动：[index, count) -> [index + collectionCount, collectionCount + count)
			var copyCount = Count - index;
			var writeIndex = index + collectionCount;
			for (var j = copyCount - 1; j != -1; --j)
				_buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(index + j)];
		}

		// 将新元素复制到指定位置
		var i = index;
		foreach (var item in collection)
		{
			_buffer[DequeIndexToBufferIndex(i)] = item;
			++i;
		}

		// 调整有效计数
		Count += collectionCount;
	}

	/// <summary>
	/// 从视图中移除一系列元素。
	/// </summary>
	/// <param name="index">范围起始位置的视图索引。</param>
	/// <param name="collectionCount">范围内的元素数量。必须大于 0 且小于或等于 <see cref="Count" />。</param>
	private void DoRemoveRange(int index, int collectionCount)
	{
		if (index == 0)
		{
			// 从开头移除：旋转到新视图
			PostIncrement(collectionCount);
			Count -= collectionCount;
			return;
		}
		else if (index == Count - collectionCount)
		{
			// 从末尾移除：修剪现有视图
			Count -= collectionCount;
			return;
		}

		if ((index + (collectionCount / 2)) < Count / 2)
		{
			// 从列表的前半部分移除

			// 将较低的元素向上移动：[0, index) -> [collectionCount, collectionCount + index)
			var copyCount = index;
			var writeIndex = collectionCount;
			for (var j = copyCount - 1; j != -1; --j)
				_buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];

			// 旋转到新视图
			PostIncrement(collectionCount);
		}
		else
		{
			// 从列表的后半部分移除

			// 将较高的元素向下移动：[index + collectionCount, count) -> [index, count - collectionCount)
			var copyCount = Count - collectionCount - index;
			var readIndex = index + collectionCount;
			for (var j = 0; j != copyCount; ++j)
				_buffer[DequeIndexToBufferIndex(index + j)] = _buffer[DequeIndexToBufferIndex(readIndex + j)];
		}

		// 调整有效计数
		Count -= collectionCount;
	}

	/// <summary>
	/// 必要时将容量加倍以为一个新元素腾出空间。此方法返回时，<see cref="IsFull" /> 为 false。
	/// </summary>
	private void EnsureCapacityForOneElement()
	{
		if (IsFull)
		{
			Capacity = (Capacity == 0) ? 1 : Capacity * 2;
		}
	}

	/// <summary>
	/// 将单个元素插入此双端队列的后端。
	/// </summary>
	/// <param name="value">要插入的元素。</param>
	public void AddToBack(T value)
	{
		EnsureCapacityForOneElement();
		DoAddToBack(value);
	}

	/// <summary>
	/// 将单个元素插入此双端队列的前端。
	/// </summary>
	/// <param name="value">要插入的元素。</param>
	public void AddToFront(T value)
	{
		EnsureCapacityForOneElement();
		DoAddToFront(value);
	}

	/// <summary>
	/// 将元素集合插入此双端队列。
	/// </summary>
	/// <param name="index">插入集合的索引。</param>
	/// <param name="collection">要插入的元素集合。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> 不是源的有效插入点索引。</exception>
	public void InsertRange(int index, IEnumerable<T> collection)
	{
		CheckNewIndexArgument(Count, index);
		var source = collection.Reify();
		var collectionCount = source.Count;

		// "Count + collectionCount > Capacity" 的防溢出检查
		if (collectionCount > Capacity - Count)
		{
			Capacity = checked(Count + collectionCount);
		}

		if (collectionCount == 0)
		{
			return;
		}

		DoInsertRange(index, source);
	}

	/// <summary>
	/// 从此双端队列中移除一系列元素。
	/// </summary>
	/// <param name="offset">双端队列中范围起始位置的索引。</param>
	/// <param name="count">要移除的元素数量。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> 或 <paramref name="count" /> 小于 0。</exception>
	/// <exception cref="ArgumentException">范围 [<paramref name="offset" />, <paramref name="offset" /> + <paramref name="count" />) 不在 [0, <see cref="Count" />) 范围内。</exception>
	public void RemoveRange(int offset, int count)
	{
		CheckRangeArguments(Count, offset, count);

		if (count == 0)
		{
			return;
		}

		DoRemoveRange(offset, count);
	}

	/// <summary>
	/// 移除并返回此双端队列的最后一个元素。
	/// </summary>
	/// <returns>原来的最后一个元素。</returns>
	/// <exception cref="InvalidOperationException">双端队列为空。</exception>
	public T RemoveFromBack()
	{
		if (IsEmpty)
			throw new InvalidOperationException("The deque is empty.");

		return DoRemoveFromBack();
	}

	/// <summary>
	/// 移除并返回此双端队列的第一个元素。
	/// </summary>
	/// <returns>原来的第一个元素。</returns>
	/// <exception cref="InvalidOperationException">双端队列为空。</exception>
	public T RemoveFromFront()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException("The deque is empty.");
		}

		return DoRemoveFromFront();
	}

	/// <summary>
	/// 移除此双端队列中的所有元素。
	/// </summary>
	public void Clear()
	{
		_offset = 0;
		Count = 0;
	}

	/// <summary>
	/// 创建并返回一个包含此双端队列中元素的新数组。
	/// </summary>
	/// <returns>包含所有元素的新数组。</returns>
	public T[] ToArray()
	{
		var result = new T[Count];
		((ICollection<T>)this).CopyTo(result, 0);
		return result;
	}
}