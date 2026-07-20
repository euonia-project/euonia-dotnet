using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的生产者/消费者集合。
/// </summary>
/// <typeparam name="T">集合中包含的元素类型。</typeparam>
[DebuggerDisplay("Count = {_collection.Count}, MaxCount = {_maxCount}")]
[DebuggerTypeProxy(typeof(AsyncCollection<>.DebugView))]
public sealed class AsyncCollection<T>
{
	/// <summary>
	/// 底层的集合。
	/// </summary>
	private readonly IProducerConsumerCollection<T> _collection;

	/// <summary>
	/// 集合中允许的最大元素数量。
	/// </summary>
	private readonly int _maxCount;

	/// <summary>
	/// 保护集合的互斥锁。
	/// </summary>
	private readonly AsyncLock _mutex;

	/// <summary>
	/// 当集合已完成添加或不处于满状态时发出信号的条件变量。
	/// </summary>
	private readonly AsyncConditionVariable _completedOrNotFull;

	/// <summary>
	/// 当集合已完成添加或不处于空状态时发出信号的条件变量。
	/// </summary>
	private readonly AsyncConditionVariable _completedOrNotEmpty;

	/// <summary>
	/// 集合是否已被标记为完成添加。
	/// </summary>
	private bool _completed;

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者集合，使用指定的集合包装并设置最大元素数量。
	/// </summary>
	/// <param name="collection">要包装的集合。</param>
	/// <param name="maxCount">最大元素数量。必须大于零。</param>
	public AsyncCollection(IProducerConsumerCollection<T> collection, int maxCount)
	{
		collection ??= new ConcurrentQueue<T>();
		if (maxCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxCount), Resources.IDS_MAXIMUM_COUNT_MUST_BE_GREATER_THAN_ZERO);
		if (maxCount < collection.Count)
			throw new ArgumentException(Resources.IDS_MAXIMUM_COUNT_CANNOT_BE_LESS_THAN_THE_NUMBER_OF_ELEMENTS_IN_THE_COLLECTION, nameof(maxCount));
		_collection = collection;
		_maxCount = maxCount;
		_mutex = new AsyncLock();
		_completedOrNotFull = new AsyncConditionVariable(_mutex);
		_completedOrNotEmpty = new AsyncConditionVariable(_mutex);
	}

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者集合，使用指定的集合包装。
	/// </summary>
	/// <param name="collection">要包装的集合。</param>
	public AsyncCollection(IProducerConsumerCollection<T> collection)
		: this(collection, int.MaxValue)
	{
	}

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者集合，并设置最大元素数量。
	/// </summary>
	/// <param name="maxCount">最大元素数量。必须大于零。</param>
	public AsyncCollection(int maxCount)
		: this(null, maxCount)
	{
	}

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者集合。
	/// </summary>
	public AsyncCollection()
		: this(null, int.MaxValue)
	{
	}

	/// <summary>
	/// 集合是否为空。
	/// </summary>
	private bool Empty => _collection.Count == 0;

	/// <summary>
	/// 集合是否已满。
	/// </summary>
	private bool Full => _collection.Count == _maxCount;

	/// <summary>
	/// 同步将生产者/消费者集合标记为完成添加。
	/// </summary>
	public void CompleteAdding()
	{
		using (_mutex.Lock())
		{
			_completed = true;
			_completedOrNotEmpty.NotifyAll();
			_completedOrNotFull.NotifyAll();
		}
	}

	/// <summary>
	/// 尝试添加一个项。
	/// </summary>
	/// <param name="item">要添加的项。</param>
	/// <param name="cancellationToken">可用于中止添加操作的取消令牌。</param>
	/// <param name="sync">是否同步运行此方法。</param>
	internal async Task DoAddAsync(T item, CancellationToken cancellationToken, bool sync)
	{
		using (sync ? _mutex.Lock() : await _mutex.LockAsync().ConfigureAwait(false))
		{
			// 等待集合变为非满状态。
			while (Full && !_completed)
			{
				if (sync)
					_completedOrNotFull.Wait(cancellationToken);
				else
					await _completedOrNotFull.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			// 如果队列已被标记为完成，则中止。
			if (_completed)
				throw new InvalidOperationException("Add failed; the producer/consumer collection has completed adding.");

			if (!_collection.TryAdd(item))
				throw new InvalidOperationException("Add failed; the add to the underlying collection failed.");

			_completedOrNotEmpty.Notify();
		}
	}

	/// <summary>
	/// 向生产者/消费者集合中添加一个项。如果生产者/消费者集合已完成添加或底层集合拒绝了该项，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要添加的项。</param>
	/// <param name="cancellationToken">可用于中止添加操作的取消令牌。</param>
	public Task AddAsync(T item, CancellationToken cancellationToken) => DoAddAsync(item, cancellationToken, sync: false);

	/// <summary>
	/// 向生产者/消费者集合中添加一个项。如果生产者/消费者集合已完成添加或底层集合拒绝了该项，则抛出 <see cref="InvalidOperationException"/>。此方法可能会阻塞调用线程。
	/// </summary>
	/// <param name="item">要添加的项。</param>
	/// <param name="cancellationToken">可用于中止添加操作的取消令牌。</param>
	public void Add(T item, CancellationToken cancellationToken) => DoAddAsync(item, cancellationToken, sync: true).WaitAndUnwrapException(cancellationToken);

	/// <summary>
	/// 向生产者/消费者集合中添加一个项。如果生产者/消费者集合已完成添加或底层集合拒绝了该项，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要添加的项。</param>
	public Task AddAsync(T item) => AddAsync(item, CancellationToken.None);

	/// <summary>
	/// 向生产者/消费者集合中添加一个项。如果生产者/消费者集合已完成添加或底层集合拒绝了该项，则抛出 <see cref="InvalidOperationException"/>。此方法可能会阻塞调用线程。
	/// </summary>
	/// <param name="item">要添加的项。</param>
	public void Add(T item) => Add(item, CancellationToken.None);

	/// <summary>
	/// 等待直到有项可供取出。如果生产者/消费者集合已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止等待的取消令牌。</param>
	/// <param name="sync">是否同步运行此方法。</param>
	private async Task<bool> DoOutputAvailableAsync(CancellationToken cancellationToken, bool sync)
	{
		using (sync ? _mutex.Lock() : await _mutex.LockAsync().ConfigureAwait(false))
		{
			while (Empty && !_completed)
			{
				if (sync)
					_completedOrNotEmpty.Wait(cancellationToken);
				else
					await _completedOrNotEmpty.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			return !Empty;
		}
	}

	/// <summary>
	/// 异步等待直到有项可供取出。如果生产者/消费者集合已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止异步等待的取消令牌。</param>
	public Task<bool> OutputAvailableAsync(CancellationToken cancellationToken) => DoOutputAvailableAsync(cancellationToken, sync: false);

	/// <summary>
	/// 异步等待直到有项可供取出。如果生产者/消费者集合已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	public Task<bool> OutputAvailableAsync() => OutputAvailableAsync(CancellationToken.None);

	/// <summary>
	/// 同步等待直到有项可供取出。如果生产者/消费者集合已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止等待的取消令牌。</param>
	public bool OutputAvailable(CancellationToken cancellationToken) => DoOutputAvailableAsync(cancellationToken, sync: true).WaitAndUnwrapException();

	/// <summary>
	/// 同步等待直到有项可供取出。如果生产者/消费者集合已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	public bool OutputAvailable() => OutputAvailable(CancellationToken.None);

	/// <summary>
	/// 提供生产者/消费者集合中项的（同步）消费可枚举序列。
	/// </summary>
	/// <param name="cancellationToken">可用于中止同步枚举的取消令牌。</param>
	public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
	{
		while (true)
		{
			T item;
			try
			{
				item = Take(cancellationToken);
			}
			catch (InvalidOperationException)
			{
				yield break;
			}

			yield return item;
		}
	}

	/// <summary>
	/// 提供生产者/消费者集合中项的（同步）消费可枚举序列。
	/// </summary>
	public IEnumerable<T> GetConsumingEnumerable()
	{
		return GetConsumingEnumerable(CancellationToken.None);
	}

	/// <summary>
	/// 尝试取出一个项。
	/// </summary>
	/// <param name="cancellationToken">可用于中止取出操作的取消令牌。</param>
	/// <param name="sync">是否同步运行此方法。</param>
	/// <exception cref="InvalidOperationException">集合已被标记为完成添加且为空。</exception>
	private async Task<T> DoTakeAsync(CancellationToken cancellationToken, bool sync)
	{
		using (sync ? _mutex.Lock() : await _mutex.LockAsync().ConfigureAwait(false))
		{
			while (Empty && !_completed)
			{
				if (sync)
					_completedOrNotEmpty.Wait(cancellationToken);
				else
					await _completedOrNotEmpty.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			if (_completed && Empty)
				throw new InvalidOperationException("Take failed; the producer/consumer collection has completed adding and is empty.");

			if (!_collection.TryTake(out T item))
				throw new InvalidOperationException("Take failed; the take from the underlying collection failed.");

			_completedOrNotFull.Notify();
			return item;
		}
	}

	/// <summary>
	/// 从生产者/消费者集合中取出一个项并返回。如果生产者/消费者集合已完成添加且为空，或底层集合的取出操作失败，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止取出操作的取消令牌。</param>
	public Task<T> TakeAsync(CancellationToken cancellationToken) => DoTakeAsync(cancellationToken, sync: false);

	/// <summary>
	/// 从生产者/消费者集合中取出一个项并返回。如果生产者/消费者集合已完成添加且为空，或底层集合的取出操作失败，则抛出 <see cref="InvalidOperationException"/>。此方法可能会阻塞调用线程。
	/// </summary>
	/// <param name="cancellationToken">可用于中止取出操作的取消令牌。</param>
	public T Take(CancellationToken cancellationToken) => DoTakeAsync(cancellationToken, sync: true).WaitAndUnwrapException();

	/// <summary>
	/// 从生产者/消费者集合中取出一个项并返回。如果生产者/消费者集合已完成添加且为空，或底层集合的取出操作失败，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	public Task<T> TakeAsync() => TakeAsync(CancellationToken.None);

	/// <summary>
	/// 从生产者/消费者集合中取出一个项并返回。如果生产者/消费者集合已完成添加且为空，或底层集合的取出操作失败，则抛出 <see cref="InvalidOperationException"/>。此方法可能会阻塞调用线程。
	/// </summary>
	public T Take() => Take(CancellationToken.None);

	[DebuggerNonUserCode]
	internal sealed class DebugView
	{
		private readonly AsyncCollection<T> _collection;

		public DebugView(AsyncCollection<T> collection)
		{
			_collection = collection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items => _collection._collection.ToArray();
	}
}
