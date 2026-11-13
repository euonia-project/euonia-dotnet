using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

/// <summary>
/// 一种专用的 <see cref="Dictionary{TKey, TValue}"/> 实现，供消息信使类型使用。
/// </summary>
/// <typeparam name="TKey">字典中键的类型。</typeparam>
/// <typeparam name="TValue">字典中值的类型。</typeparam>
[DebuggerDisplay("Count = {Count}")]
internal class TypeDictionary<TKey, TValue> : ITypeDictionary<TKey, TValue>
	where TKey : IEquatable<TKey>
	where TValue : class
{
	/// <summary>
	/// 指示空闲链表起始位置的索引。
	/// </summary>
	private const int START_OF_FREE_LIST = -3;

	/// <summary>
	/// 存储在 <see cref="_entries"/> 中的 <see cref="Entry"/> 项基于 1 的索引数组。
	/// </summary>
	private int[] _buckets;

	/// <summary>
	/// 当前存储的键值对数组（即每个哈希分组对应的列表）。
	/// </summary>
	private Entry[] _entries;

	/// <summary>
	/// 用于在进行查找时加速定位目标桶的系数。
	/// </summary>
	private ulong _fastModMultiplier;

	/// <summary>
	/// 映射中当前存储的项数。
	/// </summary>
	private int _count;

	/// <summary>
	/// <see cref="_entries"/> 中空闲链表起始位置基于 1 的索引。
	/// </summary>
	private int _freeList;

	/// <summary>
	/// 空项的总数。
	/// </summary>
	private int _freeCount;

	/// <summary>
	/// 初始化 <see cref="ITypeDictionary{TKey,TValue}"/> 类的新实例。
	/// </summary>
	public TypeDictionary()
	{
		Initialize(0);
	}

	/// <inheritdoc/>
	public int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _count - _freeCount;
	}

	/// <inheritdoc/>
	public TValue this[TKey key]
	{
		get
		{
			ref var value = ref FindValue(key);

			if (!Unsafe.IsNullRef(ref value))
			{
				return value;
			}

			ThrowArgumentExceptionForKeyNotFound(key);

			return default!;
		}
	}

	/// <inheritdoc/>
	public void Clear()
	{
		var count = _count;

		if (count > 0)
		{
			Array.Clear(_buckets!);

			_count = 0;
			_freeList = -1;
			_freeCount = 0;

			Array.Clear(_entries!, 0, count);
		}
	}

	/// <summary>
	/// 检查字典中是否包含具有指定键的键值对。
	/// </summary>
	/// <param name="key">要查找的键。</param>
	/// <returns>该键是否存在于字典中。</returns>
	public bool ContainsKey(TKey key)
	{
		return !Unsafe.IsNullRef(ref FindValue(key));
	}

	/// <summary>
	/// 获取指定键的值（如果存在）。
	/// </summary>
	/// <param name="key">要查找的键。</param>
	/// <param name="value">找到的值，否则为 <see langword="default"/>。</param>
	/// <returns>该键是否存在。</returns>
	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		ref var valRef = ref FindValue(key);

		if (!Unsafe.IsNullRef(ref valRef))
		{
			value = valRef;
			return true;
		}

		value = default;

		return false;
	}

	/// <inheritdoc/>
	public bool TryRemove(TKey key)
	{
		var hashCode = (uint)key.GetHashCode();
		ref var bucket = ref GetBucket(hashCode);
		var entries = _entries;
		var last = -1;
		var i = bucket - 1;

		while (i >= 0)
		{
			ref var entry = ref entries[i];

			if (entry.HashCode == hashCode && entry.Key.Equals(key))
			{
				if (last < 0)
				{
					bucket = entry.Next + 1;
				}
				else
				{
					entries[last].Next = entry.Next;
				}

				entry.Next = START_OF_FREE_LIST - _freeList;

				if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
				{
					entry.Key = default!;
				}

				if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
				{
					entry.Value = default!;
				}

				_freeList = i;
				_freeCount++;

				return true;
			}

			last = i;
			i = entry.Next;
		}

		return false;
	}

	/// <summary>
	/// 获取指定键的值；如果键不存在，则添加一个条目并返回该值的引用。这使得
	/// 可以在单次查找操作中添加或更新值。
	/// </summary>
	/// <param name="key">要查找的键。</param>
	/// <returns>对新的或现有值的引用。</returns>
	public ref TValue GetOrAddValueRef(TKey key)
	{
		var entries = _entries;
		var hashCode = (uint)key.GetHashCode();
		ref var bucket = ref GetBucket(hashCode);
		var i = bucket - 1;

		while (true)
		{
			if ((uint)i >= (uint)entries.Length)
			{
				break;
			}

			if (entries[i].HashCode == hashCode && entries[i].Key.Equals(key))
			{
				return ref entries[i].Value!;
			}

			i = entries[i].Next;
		}

		int index;

		if (_freeCount > 0)
		{
			index = _freeList;

			_freeList = START_OF_FREE_LIST - entries[_freeList].Next;
			_freeCount--;
		}
		else
		{
			var count = _count;

			if (count == entries.Length)
			{
				Resize();
				bucket = ref GetBucket(hashCode);
			}

			index = count;

			_count = count + 1;

			entries = _entries;
		}

		ref var entry = ref entries![index];

		entry.HashCode = hashCode;
		entry.Next = bucket - 1;
		entry.Key = key;
		entry.Value = default!;
		bucket = index + 1;

		return ref entry.Value!;
	}

	/// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// <see cref="ITypeDictionary{TKey,TValue}"/> 的枚举器。
	/// </summary>
	public ref struct Enumerator
	{
		/// <summary>
		/// 正在枚举的条目。
		/// </summary>
		private readonly Entry[] _entries;

		/// <summary>
		/// 当前枚举索引。
		/// </summary>
		private int _index;

		/// <summary>
		/// 当前字典计数。
		/// </summary>
		private readonly int _count;

		/// <summary>
		/// 创建新的 <see cref="Enumerator"/> 实例。
		/// </summary>
		/// <param name="dictionary">要枚举的输入字典。</param>
		internal Enumerator(TypeDictionary<TKey, TValue> dictionary)
		{
			_entries = dictionary._entries;
			_index = 0;
			_count = dictionary._count;
		}

		/// <inheritdoc cref="IEnumerator.MoveNext"/>
		public bool MoveNext()
		{
			while ((uint)_index < (uint)_count)
			{
				// 我们需要预先递增当前索引，以便即使用户不访问枚举器中的任何可用属性，
				// 也能正确跟踪字典中的当前位置。由于存在这种可能性，我们不能依赖这些属性
				// 在再次调用 MoveNext 之前递增索引。我们在此舍弃标准枚举器 API 接口，
				// 改为直接公开 Key/Value 属性并尽量减少内存复制。出于同样的原因，我们也
				// 移除了此处的 KeyValuePair<TKey, TValue> 字段，而是依赖这些属性从索引属性
				// 所指向的当前条目中按需直接访问目标实例（为抵消此处的递增而向后调整）。
				if (_entries![_index++].Next >= -1)
				{
					return true;
				}
			}

			_index = _count + 1;

			return false;
		}

		/// <summary>
		/// 获取当前键。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TKey GetKey()
		{
			return _entries[_index - 1].Key;
		}

		/// <summary>
		/// 获取当前值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TValue GetValue()
		{
			return _entries[_index - 1].Value!;
		}
	}

	/// <summary>
	/// 获取指定键的值，或。
	/// </summary>
	/// <param name="key">要查找的键。</param>
	/// <returns>对现有值的引用。</returns>
	private unsafe ref TValue FindValue(TKey key)
	{
		ref var entry = ref *(Entry*)null;

		var hashCode = (uint)key.GetHashCode();
		var i = GetBucket(hashCode);
		var entries = _entries;

		i--;
		do
		{
			if ((uint)i >= (uint)entries.Length)
			{
				goto ReturnNotFound;
			}

			entry = ref entries[i];

			if (entry.HashCode == hashCode && entry.Key.Equals(key))
			{
				goto ReturnFound;
			}

			i = entry.Next;
		}
		while (true);

		ReturnFound:
		return ref entry.Value!;

		ReturnNotFound:
		return ref *(TValue*)(void*)0;
	}

	/// <summary>
	/// 初始化当前实例。
	/// </summary>
	/// <param name="capacity">目标容量。</param>
	[MemberNotNull(nameof(_buckets), nameof(_entries))]
	private void Initialize(int capacity)
	{
		var size = HashHelpers.GetPrime(capacity);
		var buckets = new int[size];
		var entries = new Entry[size];

		_freeList = -1;
		_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
		_buckets = buckets;
		_entries = entries;
	}

	/// <summary>
	/// 调整当前字典的大小以减少冲突数量
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Resize()
	{
		var newSize = HashHelpers.ExpandPrime(_count);
		var entries = new Entry[newSize];
		var count = _count;

		Array.Copy(_entries, entries, count);

		_buckets = new int[newSize];
		_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);

		for (var i = 0; i < count; i++)
		{
			if (entries[i].Next >= -1)
			{
				ref var bucket = ref GetBucket(entries[i].HashCode);

				entries[i].Next = bucket - 1;
				bucket = i + 1;
			}
		}

		_entries = entries;
	}

	/// <summary>
	/// 根据输入哈希码获取目标桶的引用。
	/// </summary>
	/// <param name="hashCode">输入哈希码。</param>
	/// <returns>对目标桶的引用。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref int GetBucket(uint hashCode)
	{
		var buckets = _buckets!;

		return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
	}

	/// <summary>
	/// 表示映射条目的类型，即给定列表中的节点。
	/// </summary>
	private struct Entry
	{
		/// <summary>
		/// <see cref="Key"/> 的缓存哈希码；
		/// </summary>
		public uint HashCode;

		/// <summary>
		/// 链中下一个条目基于 0 的索引：-1 表示链结束，
		/// 同时通过改变符号并减去 3 来编码此条目本身是否属于空闲链表，
		/// 因此 -2 表示空闲链表结束，-3 表示索引 0 但在空闲链表上，-4 表示索引 1 但在空闲链表上，依此类推。
		/// </summary>
		public int Next;

		/// <summary>
		/// 当前节点中值的键。
		/// </summary>
		public TKey Key;

		/// <summary>
		/// 当前节点中的值（如果存在）。
		/// </summary>
		public TValue Value;
	}

	/// <summary>
	/// 当尝试加载缺少键的元素时引发 <see cref="ArgumentException"/>。
	/// </summary>
	private static void ThrowArgumentExceptionForKeyNotFound(TKey key)
	{
		throw new ArgumentException(string.Format(Resources.IDS_KEY_NOT_PRESENT, key));
	}
}
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type