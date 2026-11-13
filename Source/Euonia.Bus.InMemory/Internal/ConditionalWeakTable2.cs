using System.Diagnostics.CodeAnalysis;
using Nerosoft.Euonia.Bus.InMemory;

namespace System.Runtime.CompilerServices;

/// <summary>
/// 一个自定义的 <see cref="ConditionalWeakTable{TKey, TValue}"/> 实例，专门针对
/// <see cref="WeakReferenceMessenger"/> 的使用进行了优化。特别是，它提供对已存储项的零分配枚举。
/// </summary>
/// <typeparam name="TKey">要存储在表中的项的键。</typeparam>
/// <typeparam name="TValue">要存储在表中的值。</typeparam>
internal sealed class ConditionalWeakTable2<TKey, TValue>
	where TKey : class
	where TValue : class
{
	/// <summary>
	/// 表的初始长度。必须是二的幂。
	/// </summary>
	private const int INITIAL_CAPACITY = 8;
	
	/// <summary>
	/// 此锁保护表中数据的所有变更操作。读取方不获取此锁。
	/// </summary>
	private readonly object _lockObject;

	/// <summary>
	/// 表的实际存储；随着表增长而替换。
	/// </summary>
	private volatile Container _container;

	/// <summary>
	/// 初始化 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 类的新实例。
	/// </summary>
	public ConditionalWeakTable2()
	{
		_lockObject = new object();
		_container = new Container(this);
	}

	/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		return _container.TryGetValueWorker(key, out value);
	}

	/// <summary>
	/// 尝试向表中添加一个新的键值对。
	/// </summary>
	/// <param name="key">要添加的键。</param>
	/// <param name="value">要与键关联的值。</param>
	public bool TryAdd(TKey key, TValue? value)
	{
		lock (_lockObject)
		{
			var entryIndex = _container.FindEntry(key, out _);

			if (entryIndex != -1)
			{
				return false;
			}

			CreateEntry(key, value);

			return true;
		}
	}

	/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.Remove(TKey)"/>
	public bool Remove(TKey key)
	{
		lock (_lockObject)
		{
			return _container.Remove(key);
		}
	}

	/// <inheritdoc cref="ConditionalWeakTable{TKey, TValue}.GetValue(TKey, ConditionalWeakTable{TKey, TValue}.CreateValueCallback)"/>
	[UnconditionalSuppressMessage(
		"ReflectionAnalysis",
		"IL2091",
		Justification = "ConditionalWeakTable<TKey, TValue> is only referenced to reuse the callback delegate type, but no value is ever created through reflection.")]
	public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
	{
		return TryGetValue(key, out var existingValue) ? existingValue : GetValueLocked(key, createValueCallback);
	}

	/// <summary>
	/// 在锁的保护下实现 <see cref="GetValue(TKey, ConditionalWeakTable{TKey, TValue}.CreateValueCallback)"/> 的功能。
	/// </summary>
	/// <param name="key">输入的键。</param>
	/// <param name="createValueCallback">用于创建新项的调用方回调。</param>
	/// <returns>要存储的新 <typeparamref name="TValue"/> 项。</returns>
	[UnconditionalSuppressMessage(
		"ReflectionAnalysis",
		"IL2091",
		Justification = "ConditionalWeakTable<TKey, TValue> is only referenced to reuse the callback delegate type, but no value is ever created through reflection.")]
	private TValue GetValueLocked(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
	{
		// 如果执行到这里，说明该键不在表中。在锁外调用回调
		// 为该键生成新值。
		var newValue = createValueCallback(key);

		lock (_lockObject)
		{
			// 既然已获取锁，必须重新检查，以防在添加该键的竞争中被抢先
			if (_container.TryGetValueWorker(key, out var existingValue))
			{
				return existingValue;
			}
			else
			{
				// 已在锁内确认我们在添加该键的竞争中获胜。现在添加它
				CreateEntry(key, newValue);

				return newValue;
			}
		}
	}

	/// <summary>
	/// 获取用于遍历当前表项的结构化枚举器；调用方必须在 <see langword="using"/> 中释放它。
	/// </summary>
	/// <returns>预先持有表锁、在释放前保证表结构稳定的枚举器。</returns>
	/// <remarks>
	/// 为减少逐项加锁开销，枚举期间会一直持有内部锁，因此不适合在枚举过程中执行可能阻塞或重入的操作。
	/// </remarks>
	public Enumerator GetEnumerator()
	{
		// 这是针对此自定义表的一项优化，依赖于枚举器在消息信使类型中的使用方式。
		// 具体而言，枚举器始终在 using 块中使用，这意味着 Dispose() 保证一定会被执行。
		// 鉴于我们无法移除表的内部锁（需要它来确保容器被复活时的一致性，见下文），
		// 加速迭代的方案是避免在每次调用 MoveNext() 时反复获取和释放锁。
		// 在此特定场景下这样做没有问题，因为我们是这些枚举器的唯一使用者，
		// 所以无需担心枚举时阻塞其他线程。因此，这里我们预先获取锁并覆盖
		// 枚举器的整个生命周期，只在完成后释放一次。
		Monitor.Enter(_lockObject);

		return new(this);
	}

	/// <summary>
	/// 为当前 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例提供枚举器。
	/// </summary>
	public ref struct Enumerator
	{
		/// <summary>
		/// 父表，在释放时设置为 null。
		/// </summary>
		private ConditionalWeakTable2<TKey, TValue> _table;

		/// <summary>
		/// 容器中应被枚举的最后一个索引。
		/// </summary>
		private readonly int _maxIndexInclusive;

		/// <summary>
		/// 容器中的当前索引。
		/// </summary>
		private int _currentIndex;

		/// <summary>
		/// 当前键（如果可用）。
		/// </summary>
		private TKey _key;

		/// <summary>
		/// 当前值（如果可用）。
		/// </summary>
		private TValue _value;

		/// <summary>
		/// 初始化 <see cref="Enumerator"/> 类的新实例。
		/// </summary>
		/// <param name="table">正在被枚举的输入 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例。</param>
		public Enumerator(ConditionalWeakTable2<TKey, TValue> table)
		{
			// 存储对父表的引用并增加其活动枚举器计数
			_table = table;

			var container = table._container;

			if (container is null || container.FirstFreeEntry == 0)
			{
				// 最大索引与当前索引相同，以防止枚举
				_maxIndexInclusive = -1;
			}
			else
			{
				// 存储要枚举的最大索引
				_maxIndexInclusive = container.FirstFreeEntry - 1;
			}

			_currentIndex = -1;
			_key = null!;
			_value = null!;
		}

		/// <inheritdoc cref="IDisposable.Dispose"/>
		public void Dispose()
		{
			// 释放锁
			Monitor.Exit(_table._lockObject);

			_table = null!;

			// 确保不会不必要地使最后一个当前项保持存活
			_key = null!;
			_value = null!;
		}

		/// <inheritdoc cref="IEnumerator.MoveNext"/>
		public bool MoveNext()
		{
			// 必须从表中获取当前容器。自我们获取枚举器以来它可能已经改变，
			// 但由于至少存在一个活动枚举器，索引到键值对的映射不应改变。
			// 如果表（或更确切地说其当时的容器）已被终结，则此值为 null。
			var c = _table._container;

			var currentIndex = _currentIndex;
			var maxIndexInclusive = _maxIndexInclusive;

			// 我们已获得容器。查找下一个要返回的项（如果存在）。需要循环，
			// 因为可能尝试获取已被移除或回收的项，此时需重试。
			while (currentIndex < maxIndexInclusive)
			{
				currentIndex++;

				if (c.TryGetEntry(currentIndex, out var key, out var value))
				{
					_key = key;
					_value = value;
					_currentIndex = currentIndex;

					return true;
				}
			}

			_currentIndex = currentIndex;

			return false;
		}

		/// <summary>
		/// 获取当前键。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TKey GetKey()
		{
			return _key!;
		}

		/// <summary>
		/// 获取当前值。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TValue GetValue()
		{
			return _value!;
		}
	}

	/// <summary>
	/// 添加新键值对的辅助方法。如果容器已满，将调整其大小。
	/// </summary>
	/// <param name="key">新项的键。</param>
	/// <param name="value">新项的值。</param>
	private void CreateEntry(TKey key, TValue? value)
	{
		var container = _container;

		if (!container.HasCapacity)
		{
			_container = container = container.Resize();
		}

		container.CreateEntryNoResize(key, value);
	}

	/// <summary>
	/// <see cref="ConditionalWeakTable2{TKey, TValue}"/> 实例中的单个项。
	/// </summary>
	private struct Entry
	{
		/// <summary>
		/// 使用对键的弱引用和对值的强引用来保存键和值，仅当不经过值即可访问键时才会遍历该值。
		/// </summary>
		public DependentHandle DepHnd;

		/// <summary>
		/// 键的哈希码的缓存副本。
		/// </summary>
		public int HashCode;

		/// <summary>
		/// 下一个项的索引，如果是最后一项则为 -1。
		/// </summary>
		public int Next;
	}

	/// <summary>
	/// Container 保存表的实际数据。Container 的给定实例始终具有相同的容量。当需要更大容量时，
	/// 我们会创建一个新的 Container，将旧的复制到新的中，并丢弃旧的。这有助于实现对表的无锁读取，
	/// 因为读取方永远不需要处理因重新哈希而导致的项移动。
	/// </summary>
	private sealed class Container
	{
		/// <summary>
		/// 与此容器关联的 <see cref="ConditionalWeakTable2{TKey, TValue}"/>。
		/// </summary>
		private readonly ConditionalWeakTable2<TKey, TValue> _parent;

		/// <summary>
		/// <c>buckets[hashcode &amp; (buckets.Length - 1)]</c> 包含桶中第一个项的索引（如果为空则为 -1）。
		/// </summary>
		private int[] _buckets;

		/// <summary>
		/// 包含已存储依赖句柄的表项
		/// </summary>
		private Entry[] _entries;

		/// <summary>
		/// <c>firstFreeEntry &lt; entries.Length => table</c> 表示表有容量，项从表的底部开始增长。
		/// </summary>
		private int _firstFreeEntry;

		/// <summary>
		/// 标志，用于检测是否因 OOM 或其他后台异常而使我们退出锁。
		/// </summary>
		private bool _invalid;

		/// <summary>
		/// 在首次终结时设置为 true
		/// </summary>
		private bool _finalized;

		/// <summary>
		/// 用于确保下一个分配的容器在此容器被 GC 之前不被终结。
		/// </summary>
		private volatile object? _oldKeepAlive;

		/// <summary>
		/// 初始化 <see cref="Container"/> 类的新实例。
		/// </summary>
		/// <param name="parent">与当前实例关联的输入 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 对象。</param>
		internal Container(ConditionalWeakTable2<TKey, TValue> parent)
		{
			_buckets = new int[INITIAL_CAPACITY];

			for (var i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = -1;
			}

			_entries = new Entry[INITIAL_CAPACITY];

			// 仅在所有分配成功完成之后才存储父对象。
			// 否则，在增长或清空容器期间，可能会分配一个新的 Container，
			// 它在构造中途失败（OOM），但仍会被终结，
			// 并最终清除关联 CWT 中存在的其他容器。
			_parent = parent;
		}

		/// <summary>
		/// 初始化 <see cref="Container"/> 类的新实例。
		/// </summary>
		/// <param name="parent">与当前实例关联的输入 <see cref="ConditionalWeakTable2{TKey, TValue}"/> 对象。</param>
		/// <param name="buckets">桶数组。</param>
		/// <param name="entries">项数组。</param>
		/// <param name="firstFreeEntry">第一个空闲项的索引。</param>
		private Container(ConditionalWeakTable2<TKey, TValue> parent, int[] buckets, Entry[] entries, int firstFreeEntry)
		{
			_parent = parent;
			_buckets = buckets;
			_entries = entries;
			_firstFreeEntry = firstFreeEntry;
		}

		/// <summary>
		/// 获取当前容器的容量。
		/// </summary>
		internal bool HasCapacity => _firstFreeEntry < _entries.Length;

		/// <summary>
		/// 获取第一个空闲项的索引。
		/// </summary>
		internal int FirstFreeEntry => _firstFreeEntry;

		/// <summary>
		/// 添加新键值对的辅助方法。容器绝不能已满。
		/// </summary>
		internal void CreateEntryNoResize(TKey key, TValue? value)
		{
			VerifyIntegrity();

			_invalid = true;

			var hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
			var newEntry = _firstFreeEntry++;

			_entries[newEntry].HashCode = hashCode;
			_entries[newEntry].DepHnd = new DependentHandle(key, value);

			var bucket = hashCode & (_buckets.Length - 1);

			_entries[newEntry].Next = _buckets[bucket];

			// 此写入必须是易失的，因为我们可能与并发读取方竞争。如果它们
			// 看到新的项，也必须看到此方法中更早的所有写入。
			Volatile.Write(ref _buckets[bucket], newEntry);

			_invalid = false;
		}

		/// <summary>
		/// 查找键值对的辅助方法。必须持有锁。
		/// </summary>
		internal bool TryGetValueWorker(TKey key, [MaybeNullWhen(false)] out TValue value)
		{
			var entryIndex = FindEntry(key, out var secondary);

			value = Unsafe.As<TValue>(secondary);

			return entryIndex != -1;
		}

		/// <summary>
		/// 如果未找到则返回 -1（如果键在 FindEntry 期间过期，可视为“未找到”）。
		/// 必须持有锁，或准备好持锁重试搜索。
		/// </summary>
		/// <remarks>此方法要求 <paramref name="value"/> 位于堆栈上才能被正确跟踪。</remarks>
		internal int FindEntry(TKey key, out object? value)
		{
			var hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
			var bucket = hashCode & (_buckets.Length - 1);

			for (var entriesIndex = Volatile.Read(ref _buckets[bucket]); entriesIndex != -1; entriesIndex = _entries[entriesIndex].Next)
			{
				if (_entries[entriesIndex].HashCode == hashCode)
				{
					// if (_entries[entriesIndex].depHnd.UnsafeGetTargetAndDependent(out value) == key)
					(var oKey, value) = _entries[entriesIndex].DepHnd.TargetAndDependent;

					if (oKey == key)
					{
						// 确保在访问 DependentHandle 时不被终结
						GC.KeepAlive(this);

						return entriesIndex;
					}
				}
			}

			// 确保在访问 DependentHandle 时不被终结
			GC.KeepAlive(this);

			value = null;

			return -1;
		}

		/// <summary>
		/// 获取指定项索引处的项。
		/// </summary>
		internal bool TryGetEntry(int index, [NotNullWhen(true)] out TKey key, [MaybeNullWhen(false)] out TValue value)
		{
			if (index < _entries.Length)
			{
				// object? oKey = entries[index].depHnd.UnsafeGetTargetAndDependent(out object? oValue);
				var (oKey, oValue) = _entries[index].DepHnd.TargetAndDependent;

				// 确保在访问 DependentHandle 时不被终结
				GC.KeepAlive(this);

				if (oKey != null)
				{
					key = Unsafe.As<TKey>(oKey);
					value = Unsafe.As<TValue>(oValue)!;

					return true;
				}
			}

			key = default!;
			value = default;

			return false;
		}

		/// <summary>
		/// 如果存在，从表中移除指定的键。
		/// </summary>
		internal bool Remove(TKey key)
		{
			VerifyIntegrity();

			var entryIndex = FindEntry(key, out _);

			if (entryIndex != -1)
			{
				RemoveIndex(entryIndex);

				return true;
			}

			return false;
		}

		/// <summary>
		/// 移除指定索引处的给定项。
		/// </summary>
		/// <param name="entryIndex">要移除的项的索引。</param>
		private void RemoveIndex(int entryIndex)
		{
			ref var entry = ref _entries[entryIndex];

			// 此处不释放句柄，因为我们可能与已经看到哈希码的读取方竞争。
			// 我们只是覆盖该项的哈希码，以便后续读取忽略它。
			// 句柄将在表调整大小或丢弃之后，在 Container 的终结器中释放。
			Volatile.Write(ref entry.HashCode, -1);

			// 同时清除键，以便 GC 回收该项所指向的对象
			// entry.depHnd.UnsafeSetTargetToNull();
			entry.DepHnd.Target = null;
		}

		/// <summary>
		/// 调整大小，并从桶列表中清除过期的键。必须持有 <see cref="ConditionalWeakTable2{TKey,TValue}._lockObject"/>。
		/// </summary>
		/// <remarks>
		/// 退出时 <see cref="_firstFreeEntry"/> 小于 <c>entries.Length</c>，即表至少有一个空闲项。
		/// </remarks>
		internal Container Resize()
		{
			var hasExpiredEntries = false;
			var newSize = _buckets.Length;

			// 如果存在任何过期或已移除的键，则不会调整大小
			for (var entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++)
			{
				ref var entry = ref _entries[entriesIndex];

				if (entry.HashCode == -1)
				{
					// 该项已被移除
					hasExpiredEntries = true;

					break;
				}

				if (entry.DepHnd.IsAllocated &&
				    // entry.depHnd.UnsafeGetTarget() is null)
				    entry.DepHnd.Target is null)
				{
					// 该项已过期
					hasExpiredEntries = true;

					break;
				}
			}

			if (!hasExpiredEntries)
			{
				// 此处无需检查溢出，尝试分配新数组时会抛出异常
				newSize = _buckets.Length * 2;
			}

			return Resize(newSize);
		}

		/// <summary>
		/// 使用当前项创建指定大小的新 <see cref="Container"/>。
		/// </summary>
		/// <param name="newSize">请求的新大小。</param>
		/// <returns>具有所请求大小的新 <see cref="Container"/> 实例。</returns>
		internal Container Resize(int newSize)
		{
			// 重新分配桶和项，并从头重建桶和项。
			// 这既用于清除具有过期键的项，也用于将新项放入正确的桶中。
			var newBuckets = new int[newSize];

			for (var bucketIndex = 0; bucketIndex < newBuckets.Length; bucketIndex++)
			{
				newBuckets[bucketIndex] = -1;
			}

			var newEntries = new Entry[newSize];
			var newEntriesIndex = 0;

			// 没有活动枚举器，这意味着我们希望移除过期/已移除的项来进行压缩
			for (var entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++)
			{
				ref var oldEntry = ref _entries[entriesIndex];
				var hashCode = oldEntry.HashCode;
				var depHnd = oldEntry.DepHnd;

				if (hashCode != -1 && depHnd.IsAllocated)
				{
					// if (depHnd.UnsafeGetTarget() is not null)
					if (depHnd.Target is not null)
					{
						ref var newEntry = ref newEntries[newEntriesIndex];

						// 该项正在使用且尚未过期。将其链接到相应的桶列表
						newEntry.HashCode = hashCode;
						newEntry.DepHnd = depHnd;

						var bucket = hashCode & (newBuckets.Length - 1);

						newEntry.Next = newBuckets[bucket];
						newBuckets[bucket] = newEntriesIndex;
						newEntriesIndex++;
					}
					else
					{
						// 假装该项已被移除，以便此容器的终结器清理此依赖句柄
						Volatile.Write(ref oldEntry.HashCode, -1);
					}
				}
			}

			// 创建新的容器。我们希望将释放句柄的责任从旧容器转移到新容器，
			// 同时确保在旧容器仍可能被使用时新容器不会被终结。
			// 因此，我们存储一个从旧容器到新容器的引用，
			// 这会使新容器在旧容器存活期间保持存活。
			Container newContainer = new(_parent!, newBuckets, newEntries, newEntriesIndex);

			// 一旦设置此项，旧容器的终结器将不会释放已转移的依赖句柄
			_oldKeepAlive = newContainer;

			// 确保在访问 DependentHandles 时不被终结
			GC.KeepAlive(this);

			return newContainer;
		}

		/// <summary>
		/// 验证当前实例是否有效。
		/// </summary>
		/// <exception cref="InvalidOperationException">如果当前实例无效则抛出。</exception>
		private void VerifyIntegrity()
		{
			if (_invalid)
			{
				// 本文件位于 System.Runtime.CompilerServices 命名空间，此处 Resources 会被解析为
				// System.Resources，因此必须写全限定名。
				static void Throw() => throw new InvalidOperationException(Nerosoft.Euonia.Bus.InMemory.Properties.Resources.IDS_COLLECTION_CORRUPTED_STATE);

				Throw();
			}
		}

		/// <summary>
		/// 终结当前 <see cref="Container"/> 实例。
		/// </summary>
		~Container()
		{
			// 如果容器无效，则不做任何操作，包括容器对象已分配
			// 但从未设置其关联表的情况。
			if (_invalid || _parent is null)
			{
				return;
			}

			// ConditionalWeakTable2 有可能已被复活，在这种情况下，代码可能会在此 Container
			// 被终结时访问它。我们不支持在终结后使用，但也不希望允许依赖句柄在被释放时
			// 或释放后被使用，从而可能破坏状态。为避免这种情况，如果完全有可能另一个线程
			// 通过 CWT 持有对此容器的引用，我们会移除该引用，然后重新注册以进行终结：
			// 下一次终结时，我们可以确信不存在对此对象的剩余引用，
			// 从而可以放心清理依赖句柄而无需担心状态损坏。
			if (!_finalized)
			{
				_finalized = true;

				lock (_parent._lockObject)
				{
					if (_parent._container == this)
					{
						_parent._container = null!;
					}
				}

				// 下次终结时，我们将确信没有剩余引用
				GC.ReRegisterForFinalize(this);

				return;
			}

			var entries = _entries;

			_invalid = true;
			_entries = null!;
			_buckets = null!;

			if (entries != null)
			{
				for (var entriesIndex = 0; entriesIndex < entries.Length; entriesIndex++)
				{
					// 需要在两种情况下释放句柄：
					// - 如果此容器仍拥有该依赖句柄（意味着所有权尚未转移到
					//   替代此容器的另一个容器），则应释放它。
					// - 如果此容器已移除该项，那么即使通常所有权已转移到
					//   另一个容器，已移除的项也不会转移，因此此容器必须释放它们。
					if (_oldKeepAlive is null || entries[entriesIndex].HashCode == -1)
					{
						entries[entriesIndex].DepHnd.Dispose();
					}
				}
			}
		}
	}
}