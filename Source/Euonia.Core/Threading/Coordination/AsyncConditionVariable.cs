using System.Diagnostics;
using Nerosoft.Euonia.Threading.Interop;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的条件变量。此类型使用 Mesa 风格语义（通知任务不会让出执行权）。
/// </summary>
[DebuggerDisplay("Id = {Id}, AsyncLockId = {_asyncLock.Id}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncConditionVariable
{
    /// <summary>
    /// 与此条件变量关联的锁。
    /// </summary>
    private readonly AsyncLock _asyncLock;

    /// <summary>
    /// 等待任务的队列。
    /// </summary>
    private readonly IAsyncWaitQueue<object> _queue;

    /// <summary>
    /// 此实例的半唯一标识符。如果尚未创建 ID，则为 0。
    /// </summary>
    private int _id;

    /// <summary>
    /// 用于互斥的对象。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 创建一个与异步兼容锁关联的异步兼容条件变量。
    /// </summary>
    /// <param name="asyncLock">与此条件变量关联的锁。</param>
    /// <param name="queue">用于管理等待者的等待队列。可以为 <c>null</c> 以使用默认（FIFO）队列。</param>
    internal AsyncConditionVariable(AsyncLock asyncLock, IAsyncWaitQueue<object> queue)
    {
        _asyncLock = asyncLock;
        _queue = queue ?? new DefaultAsyncWaitQueue<object>();
        _mutex = new object();
    }

    /// <summary>
    /// 创建一个与异步兼容锁关联的异步兼容条件变量。
    /// </summary>
    /// <param name="asyncLock">与此条件变量关联的锁。</param>
    public AsyncConditionVariable(AsyncLock asyncLock)
        : this(asyncLock, null)
    {
    }

    /// <summary>
    /// 获取此异步条件变量的半唯一标识符。
    /// </summary>
    public int Id => IdentifierManager<AsyncConditionVariable>.GetId(ref _id);

    /// <summary>
    /// 向正在等待此条件变量的单个任务发送信号。调用此方法时必须持有关联的锁，并且在此方法返回时锁仍将被持有。
    /// </summary>
    public void Notify()
    {
        lock (_mutex)
        {
            if (!_queue.IsEmpty)
                _queue.Dequeue();
        }
    }

    /// <summary>
    /// 向正在等待此条件变量的所有任务发送信号。调用此方法时必须持有关联的锁，并且在此方法返回时锁仍将被持有。
    /// </summary>
    public void NotifyAll()
    {
        lock (_mutex)
        {
            _queue.DequeueAll();
        }
    }

    /// <summary>
    /// 异步等待此条件变量的信号。调用此方法时必须持有关联的锁，并且在此方法返回时锁仍将被持有，即使该方法被取消。
    /// </summary>
    /// <param name="cancellationToken">用于取消此等待的取消信号。</param>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        Task task;
        lock (_mutex)
        {
            // 开始等待信号或取消。
            task = _queue.Enqueue(_mutex, cancellationToken);

            // 附加到信号或取消。
            var ret = WaitAndRetakeLockAsync(task, _asyncLock);

            // 在等待期间释放锁。
            _asyncLock.ReleaseLock();

            return ret;
        }
    }

    private static async Task WaitAndRetakeLockAsync(Task task, AsyncLock asyncLock)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            // 重新获取锁。
            await asyncLock.LockAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 异步等待此条件变量的信号。调用此方法时必须持有关联的锁，并且当返回的任务完成时锁仍将被持有。
    /// </summary>
    public Task WaitAsync()
    {
        return WaitAsync(CancellationToken.None);
    }

    /// <summary>
    /// 同步等待此条件变量的信号。此方法可能会阻塞调用线程。调用此方法时必须持有关联的锁，并且在此方法返回时锁仍将被持有，即使该方法被取消。
    /// </summary>
    /// <param name="cancellationToken">用于取消此等待的取消信号。</param>
    public void Wait(CancellationToken cancellationToken)
    {
        WaitAsync(cancellationToken).WaitAndUnwrapException(cancellationToken);
    }

    /// <summary>
    /// 同步等待此条件变量的信号。此方法可能会阻塞调用线程。调用此方法时必须持有关联的锁，并且在此方法返回时锁仍将被持有。
    /// </summary>
    public void Wait()
    {
        Wait(CancellationToken.None);
    }

    // ReSharper disable UnusedMember.Local
    [DebuggerNonUserCode]
    private sealed class DebugView
    {
        private readonly AsyncConditionVariable _cv;

        public DebugView(AsyncConditionVariable cv)
        {
            _cv = cv;
        }

        public int Id => _cv.Id;

        public AsyncLock AsyncLock => _cv._asyncLock;

        public IAsyncWaitQueue<object> WaitQueue => _cv._queue;
    }
}
