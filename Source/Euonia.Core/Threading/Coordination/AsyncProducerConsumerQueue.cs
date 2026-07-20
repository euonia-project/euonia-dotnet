using System.Diagnostics;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的生产者/消费者队列。
/// </summary>
/// <typeparam name="T">队列中包含的元素类型。</typeparam>
[DebuggerDisplay("Count = {_queue.Count}, MaxCount = {_maxCount}")]
[DebuggerTypeProxy(typeof(AsyncProducerConsumerQueue<>.DebugView))]
public sealed class AsyncProducerConsumerQueue<T>
{
	/// <summary>
	/// 底层的队列。
	/// </summary>
	private readonly Queue<T> _queue;

	/// <summary>
	/// 队列中允许的最大元素数量。
	/// </summary>
	private readonly int _maxCount;

	/// <summary>
	/// 保护 <c>_queue</c> 和 <c>_completed</c> 的互斥锁。
	/// </summary>
	private readonly AsyncLock _mutex;

	/// <summary>
	/// 当队列不处于满状态时发出信号的条件变量。
	/// </summary>
	private readonly AsyncConditionVariable _completedOrNotFull;

	/// <summary>
	/// 当队列已完成添加或不处于空状态时发出信号的条件变量。
	/// </summary>
	private readonly AsyncConditionVariable _completedOrNotEmpty;

	/// <summary>
	/// 此生产者/消费者队列是否已被标记为完成添加。
	/// </summary>
	private bool _completed;

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者队列，使用指定的初始元素和最大元素数量。
	/// </summary>
	/// <param name="collection">要放入队列的初始元素。可以为 <c>null</c> 以从空集合开始。</param>
	/// <param name="maxCount">最大元素数量。必须大于零，且大于或等于 <paramref name="collection"/> 中的元素数量。</param>
	public AsyncProducerConsumerQueue(IEnumerable<T> collection, int maxCount)
	{
		if (maxCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxCount), Resources.IDS_MAXIMUM_COUNT_MUST_BE_GREATER_THAN_ZERO);
		_queue = collection == null ? new Queue<T>() : new Queue<T>(collection);
		if (maxCount < _queue.Count)
		{
			throw new ArgumentException(Resources.IDS_MAXIMUM_COUNT_CANNOT_BE_LESS_THAN_THE_NUMBER_OF_ELEMENTS_IN_THE_COLLECTION, nameof(maxCount));
		}

		_maxCount = maxCount;

		_mutex = new AsyncLock();
		_completedOrNotFull = new AsyncConditionVariable(_mutex);
		_completedOrNotEmpty = new AsyncConditionVariable(_mutex);
	}

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者队列，使用指定的初始元素。
	/// </summary>
	/// <param name="collection">要放入队列的初始元素。可以为 <c>null</c> 以从空集合开始。</param>
	public AsyncProducerConsumerQueue(IEnumerable<T> collection)
		: this(collection, int.MaxValue)
	{
	}

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者队列，并设置最大元素数量。
	/// </summary>
	/// <param name="maxCount">最大元素数量。必须大于零。</param>
	public AsyncProducerConsumerQueue(int maxCount)
		: this(null, maxCount)
	{
	}

	/// <summary>
	/// 创建一个新的异步兼容的生产者/消费者队列。
	/// </summary>
	public AsyncProducerConsumerQueue()
		: this(null, int.MaxValue)
	{
	}

	/// <summary>
	/// 队列是否为空。此属性假定 <c>_mutex</c> 已被持有。
	/// </summary>
	private bool Empty => _queue.Count == 0;

	/// <summary>
	/// 队列是否已满。此属性假定 <c>_mutex</c> 已被持有。
	/// </summary>
	private bool Full => _queue.Count == _maxCount;

	/// <summary>
	/// 将生产者/消费者队列标记为完成添加。
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
	/// 向生产者/消费者队列入队一个项。如果生产者/消费者队列已完成添加，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要入队的项。</param>
	/// <param name="cancellationToken">可用于中止入队操作的取消令牌。</param>
	/// <param name="sync">是否同步运行此方法。</param>
	private async Task DoEnqueueAsync(T item, CancellationToken cancellationToken, bool sync)
	{
		using (sync ? _mutex.Lock() : await _mutex.LockAsync().ConfigureAwait(false))
		{
			// 等待队列变为非满状态。
			while (Full && !_completed)
			{
				if (sync)
					_completedOrNotFull.Wait(cancellationToken);
				else
					await _completedOrNotFull.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			// 如果队列已被标记为完成，则中止。
			if (_completed)
				throw new InvalidOperationException("Enqueue failed; the producer/consumer queue has completed adding.");

			_queue.Enqueue(item);
			_completedOrNotEmpty.Notify();
		}
	}

	/// <summary>
	/// 向生产者/消费者队列入队一个项。如果生产者/消费者队列已完成添加，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要入队的项。</param>
	/// <param name="cancellationToken">可用于中止入队操作的取消令牌。</param>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加。</exception>
	public Task EnqueueAsync(T item, CancellationToken cancellationToken)
	{
		return DoEnqueueAsync(item, cancellationToken, sync: false);
	}

	/// <summary>
	/// 向生产者/消费者队列入队一个项。如果生产者/消费者队列已完成添加，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要入队的项。</param>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加。</exception>
	public Task EnqueueAsync(T item)
	{
		return EnqueueAsync(item, CancellationToken.None);
	}

	/// <summary>
	/// 向生产者/消费者队列入队一个项。此方法可能会阻塞调用线程。如果生产者/消费者队列已完成添加，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要入队的项。</param>
	/// <param name="cancellationToken">可用于中止入队操作的取消令牌。</param>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加。</exception>
	public void Enqueue(T item, CancellationToken cancellationToken)
	{
		DoEnqueueAsync(item, cancellationToken, sync: true).WaitAndUnwrapException(cancellationToken);
	}

	/// <summary>
	/// 向生产者/消费者队列入队一个项。此方法可能会阻塞调用线程。如果生产者/消费者队列已完成添加，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="item">要入队的项。</param>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加。</exception>
	public void Enqueue(T item)
	{
		Enqueue(item, CancellationToken.None);
	}

	/// <summary>
	/// 等待直到有项可供出队。如果生产者/消费者队列已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止异步等待的取消令牌。</param>
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
	/// 异步等待直到有项可供出队。如果生产者/消费者队列已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止异步等待的取消令牌。</param>
	public Task<bool> OutputAvailableAsync(CancellationToken cancellationToken)
	{
		return DoOutputAvailableAsync(cancellationToken, sync: false);
	}

	/// <summary>
	/// 异步等待直到有项可供出队。如果生产者/消费者队列已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	public Task<bool> OutputAvailableAsync()
	{
		return OutputAvailableAsync(CancellationToken.None);
	}

	/// <summary>
	/// 同步等待直到有项可供出队。如果生产者/消费者队列已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止异步等待的取消令牌。</param>
	public bool OutputAvailable(CancellationToken cancellationToken)
	{
		return DoOutputAvailableAsync(cancellationToken, sync: true).WaitAndUnwrapException(cancellationToken);
	}

	/// <summary>
	/// 同步等待直到有项可供出队。如果生产者/消费者队列已完成添加且没有更多项，则返回 <c>false</c>。
	/// </summary>
	public bool OutputAvailable()
	{
		return OutputAvailable(CancellationToken.None);
	}

	/// <summary>
	/// 提供生产者/消费者队列中项的（同步）消费可枚举序列。
	/// </summary>
	/// <param name="cancellationToken">可用于中止同步枚举的取消令牌。</param>
	public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
	{
		while (true)
		{
			var result = TryDoDequeueAsync(cancellationToken, sync: true).WaitAndUnwrapException();
			if (!result.Item1)
				yield break;
			yield return result.Item2;
		}
	}

	/// <summary>
	/// 提供生产者/消费者队列中项的（同步）消费可枚举序列。
	/// </summary>
	public IEnumerable<T> GetConsumingEnumerable()
	{
		return GetConsumingEnumerable(CancellationToken.None);
	}

	/// <summary>
	/// 尝试从生产者/消费者队列中出队一个项。如果生产者/消费者队列已完成添加且为空，则返回 <c>false</c>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止出队操作的取消令牌。</param>
	/// <param name="sync">是否同步运行此方法。</param>
	private async Task<Tuple<bool, T>> TryDoDequeueAsync(CancellationToken cancellationToken, bool sync)
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
				return Tuple.Create(false, default(T));

			var item = _queue.Dequeue();
			_completedOrNotFull.Notify();
			return Tuple.Create(true, item);
		}
	}

	/// <summary>
	/// 从生产者/消费者队列中出队一个项。如果生产者/消费者队列已完成添加且为空，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止出队操作的取消令牌。</param>
	/// <param name="sync">是否同步运行此方法。</param>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加且为空。</exception>
	private async Task<T> DoDequeueAsync(CancellationToken cancellationToken, bool sync)
	{
		var result = await TryDoDequeueAsync(cancellationToken, sync).ConfigureAwait(false);
		if (result.Item1)
			return result.Item2;
		throw new InvalidOperationException("Dequeue failed; the producer/consumer queue has completed adding and is empty.");
	}

	/// <summary>
	/// 从生产者/消费者队列中出队一个项。如果生产者/消费者队列已完成添加且为空，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止出队操作的取消令牌。</param>
	/// <returns>出队的项。</returns>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加且为空。</exception>
	public Task<T> DequeueAsync(CancellationToken cancellationToken)
	{
		return DoDequeueAsync(cancellationToken, sync: false);
	}

	/// <summary>
	/// 从生产者/消费者队列中出队一个项并返回。如果生产者/消费者队列已完成添加且为空，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <returns>出队的项。</returns>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加且为空。</exception>
	public Task<T> DequeueAsync()
	{
		return DequeueAsync(CancellationToken.None);
	}

	/// <summary>
	/// 从生产者/消费者队列中出队一个项并返回。此方法可能会阻塞调用线程。如果生产者/消费者队列已完成添加且为空，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="cancellationToken">可用于中止出队操作的取消令牌。</param>
	/// <returns>出队的项。</returns>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加且为空。</exception>
	public T Dequeue(CancellationToken cancellationToken)
	{
		return DoDequeueAsync(cancellationToken, sync: true).WaitAndUnwrapException(cancellationToken);
	}

	/// <summary>
	/// 从生产者/消费者队列中出队一个项并返回。此方法可能会阻塞调用线程。如果生产者/消费者队列已完成添加且为空，则抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <returns>出队的项。</returns>
	/// <exception cref="InvalidOperationException">生产者/消费者队列已被标记为完成添加且为空。</exception>
	public T Dequeue()
	{
		return Dequeue(CancellationToken.None);
	}

	[DebuggerNonUserCode]
	internal sealed class DebugView
	{
		private readonly AsyncProducerConsumerQueue<T> _queue;

		public DebugView(AsyncProducerConsumerQueue<T> queue)
		{
			_queue = queue;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items => _queue._queue.ToArray();
	}
}
