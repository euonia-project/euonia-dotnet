using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

/// <summary>
/// A specialized <see cref="Dictionary{TKey, TValue}"/> implementation to be used with messenger types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
[DebuggerDisplay("Count = {Count}")]
internal class TypeDictionary<TKey, TValue> : ITypeDictionary<TKey, TValue>
	where TKey : IEquatable<TKey>
	where TValue : class
{
	/// <summary>
	/// The index indicating the start of a free linked list.
	/// </summary>
	private const int START_OF_FREE_LIST = -3;

	/// <summary>
	/// The array of 1-based indices for the <see cref="Entry"/> items stored in <see cref="_entries"/>.
	/// </summary>
	private int[] _buckets;

	/// <summary>
	/// The array of currently stored key-value pairs (ie. the lists for each hash group).
	/// </summary>
	private Entry[] _entries;

	/// <summary>
	/// A coefficient used to speed up retrieving the target bucket when doing lookups.
	/// </summary>
	private ulong _fastModMultiplier;

	/// <summary>
	/// The current number of items stored in the map.
	/// </summary>
	private int _count;

	/// <summary>
	/// The 1-based index for the start of the free list within <see cref="_entries"/>.
	/// </summary>
	private int _freeList;

	/// <summary>
	/// The total number of empty items.
	/// </summary>
	private int _freeCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="ITypeDictionary{TKey,TValue}"/> class.
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
#if NETSTANDARD2_0_OR_GREATER
            Array.Clear(_buckets!, 0, _buckets!.Length);
#else
			Array.Clear(_buckets!);
#endif

			_count = 0;
			_freeList = -1;
			_freeCount = 0;

			Array.Clear(_entries!, 0, count);
		}
	}

	/// <summary>
	/// Checks whether or not the dictionary contains a pair with a specified key.
	/// </summary>
	/// <param name="key">The key to look for.</param>
	/// <returns>Whether or not the key was present in the dictionary.</returns>
	public bool ContainsKey(TKey key)
	{
		return !Unsafe.IsNullRef(ref FindValue(key));
	}

	/// <summary>
	/// Gets the value if present for the specified key.
	/// </summary>
	/// <param name="key">The key to look for.</param>
	/// <param name="value">The value found, otherwise <see langword="default"/>.</param>
	/// <returns>Whether or not the key was present.</returns>
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
	/// Gets the value for the specified key, or, if the key is not present,
	/// adds an entry and returns the value by ref. This makes it possible to
	/// add or update a value in a single look up operation.
	/// </summary>
	/// <param name="key">Key to look for.</param>
	/// <returns>Reference to the new or existing value.</returns>
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
	/// Enumerator for <see cref="ITypeDictionary{TKey,TValue}"/>.
	/// </summary>
	public ref struct Enumerator
	{
		/// <summary>
		/// The entries being enumerated.
		/// </summary>
		private readonly Entry[] _entries;

		/// <summary>
		/// The current enumeration index.
		/// </summary>
		private int _index;

		/// <summary>
		/// The current dictionary count.
		/// </summary>
		private readonly int _count;

		/// <summary>
		/// Creates a new <see cref="Enumerator"/> instance.
		/// </summary>
		/// <param name="dictionary">The input dictionary to enumerate.</param>
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
				// We need to preemptively increment the current index so that we still correctly keep track
				// of the current position in the dictionary even if the users doesn't access any of the
				// available properties in the enumerator. As this is a possibility, we can't rely on one of
				// them to increment the index before MoveNext is invoked again. We ditch the standard enumerator
				// API surface here to expose the Key/Value properties directly and minimize the memory copies.
				// For the same reason, we also removed the KeyValuePair<TKey, TValue> field here, and instead
				// rely on the properties lazily accessing the target instances directly from the current entry
				// pointed at by the index property (adjusted backwards to account for the increment here).
				if (_entries![_index++].Next >= -1)
				{
					return true;
				}
			}

			_index = _count + 1;

			return false;
		}

		/// <summary>
		/// Gets the current key.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TKey GetKey()
		{
			return _entries[_index - 1].Key;
		}

		/// <summary>
		/// Gets the current value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TValue GetValue()
		{
			return _entries[_index - 1].Value!;
		}
	}

	/// <summary>
	/// Gets the value for the specified key, or.
	/// </summary>
	/// <param name="key">Key to look for.</param>
	/// <returns>Reference to the existing value.</returns>
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
		ref var value = ref entry.Value!;

		Return:
		return ref value;

		ReturnNotFound:

		value = ref *(TValue*)null;

		goto Return;
	}

	/// <summary>
	/// Initializes the current instance.
	/// </summary>
	/// <param name="capacity">The target capacity.</param>
	/// <returns></returns>
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
	/// Resizes the current dictionary to reduce the number of collisions
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
	/// Gets a reference to a target bucket from an input hashcode.
	/// </summary>
	/// <param name="hashCode">The input hashcode.</param>
	/// <returns>A reference to the target bucket.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref int GetBucket(uint hashCode)
	{
		var buckets = _buckets!;

		return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
	}

	/// <summary>
	/// A type representing a map entry, ie. a node in a given list.
	/// </summary>
	private struct Entry
	{
		/// <summary>
		/// The cached hashcode for <see cref="Key"/>;
		/// </summary>
		public uint HashCode;

		/// <summary>
		/// 0-based index of next entry in chain: -1 means end of chain
		/// also encodes whether this entry this.itself_ is part of the free list by changing sign and subtracting 3,
		/// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
		/// </summary>
		public int Next;

		/// <summary>
		/// The key for the value in the current node.
		/// </summary>
		public TKey Key;

		/// <summary>
		/// The value in the current node, if present.
		/// </summary>
		public TValue Value;
	}

	/// <summary>
	/// Throws an <see cref="ArgumentException"/> when trying to load an element with a missing key.
	/// </summary>
	private static void ThrowArgumentExceptionForKeyNotFound(TKey key)
	{
		throw new ArgumentException(string.Format(Resources.IDS_KEY_NOT_PRESENT, key));
	}
}
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type