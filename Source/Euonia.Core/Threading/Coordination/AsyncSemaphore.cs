using System.Diagnostics;
using Nerosoft.Euonia.Disposing;
using Nerosoft.Euonia.Threading.Interop;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的信号量。或者，你也可以使用 <c>SemaphoreSlim</c>。
/// </summary>
[DebuggerDisplay("Id = {Id}, CurrentCount = {_count}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncSemaphore
{
    /// <summary>
    /// 其他任务正在等待获取信号量的 TCS 队列。
    /// </summary>
    private readonly IAsyncWaitQueue<object> _queue;

    /// <summary>
    /// 将立即授予的等待次数。
    /// </summary>
    private long _count;

    /// <summary>
    /// 此实例的半唯一标识符。如果尚未创建 ID，则为 0。
    /// </summary>
    private int _id;

    /// <summary>
    /// 用于互斥的对象。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 使用指定的初始计数创建一个新的异步兼容信号量。
    /// </summary>
    /// <param name="initialCount">此信号量的初始计数。必须大于或等于零。</param>
    /// <param name="queue">用于管理等待者的等待队列。可以为 <c>null</c> 以使用默认（FIFO）队列。</param>
    internal AsyncSemaphore(long initialCount, IAsyncWaitQueue<object> queue)
    {
        _queue = queue ?? new DefaultAsyncWaitQueue<object>();
        _count = initialCount;
        _mutex = new object();
    }

    /// <summary>
    /// 使用指定的初始计数创建一个新的异步兼容信号量。
    /// </summary>
    /// <param name="initialCount">此信号量的初始计数。必须大于或等于零。</param>
    public AsyncSemaphore(long initialCount)
        : this(initialCount, null)
    {
    }

    /// <summary>
    /// 获取此异步信号量的半唯一标识符。
    /// </summary>
    public int Id => IdentifierManager<AsyncSemaphore>.GetId(ref _id);

    /// <summary>
    /// 获取此信号量当前可用的槽位数。此成员很少使用；使用此成员的代码很可能存在竞态条件。
    /// </summary>
    public long CurrentCount
    {
        get { lock (_mutex) { return _count; } }
    }

    /// <summary>
    /// 异步等待信号量中的槽位变为可用。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果已设置，此方法将尝试立即获取槽位（如果当前有可用槽位则成功）。</param>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        Task ret;
        lock (_mutex)
        {
            // 如果信号量可用，立即获取并返回。
            if (_count != 0)
            {
                --_count;
                ret = TaskConstants.Completed;
            }
            else
            {
                // 等待信号量变为可用或取消。
                ret = _queue.Enqueue(_mutex, cancellationToken);
            }
        }

        return ret;
    }

    /// <summary>
    /// 异步等待信号量中的槽位变为可用。
    /// </summary>
    public Task WaitAsync()
    {
        return WaitAsync(CancellationToken.None);
    }

    /// <summary>
    /// 同步等待信号量中的槽位变为可用。此方法可能会阻塞调用线程。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果已设置，此方法将尝试立即获取槽位（如果当前有可用槽位则成功）。</param>
    public void Wait(CancellationToken cancellationToken)
    {
        WaitAsync(cancellationToken).WaitAndUnwrapException(cancellationToken);
    }

    /// <summary>
    /// 同步等待信号量中的槽位变为可用。此方法可能会阻塞调用线程。
    /// </summary>
    public void Wait()
    {
        Wait(CancellationToken.None);
    }

    /// <summary>
    /// 释放信号量。
    /// </summary>
    /// <param name="releaseCount">要释放的次数。</param>
    public void Release(long releaseCount)
    {
        if (releaseCount == 0)
            return;

        lock (_mutex)
        {
            checked
            {
                var _ = _count + releaseCount;
            }

            while (releaseCount != 0 && !_queue.IsEmpty)
            {
                _queue.Dequeue();
                --releaseCount;
            }
            _count += releaseCount;
        }
    }

    /// <summary>
    /// 释放信号量。
    /// </summary>
    public void Release()
    {
        Release(1);
    }

    private async Task<IDisposable> DoLockAsync(CancellationToken cancellationToken)
    {
        await WaitAsync(cancellationToken).ConfigureAwait(false);
        return AnonymousDisposable.Create(Release);
    }

    /// <summary>
    /// 异步等待信号量，并返回一个在释放时释放信号量的可释放对象，从而将此信号量视作"多锁"使用。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果已设置，此方法将尝试立即获取槽位（如果当前有可用槽位则成功）。</param>
    public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken)
    {
        return new AwaitableDisposable<IDisposable>(DoLockAsync(cancellationToken));
    }

    /// <summary>
    /// 异步等待信号量，并返回一个在释放时释放信号量的可释放对象，从而将此信号量视作"多锁"使用。
    /// </summary>
    public AwaitableDisposable<IDisposable> LockAsync() => LockAsync(CancellationToken.None);

    /// <summary>
    /// 同步等待信号量，并返回一个在释放时释放信号量的可释放对象，从而将此信号量视作"多锁"使用。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果已设置，此方法将尝试立即获取槽位（如果当前有可用槽位则成功）。</param>
    public IDisposable Lock(CancellationToken cancellationToken)
    {
        Wait(cancellationToken);
        return AnonymousDisposable.Create(Release);
    }

    /// <summary>
    /// 同步等待信号量，并返回一个在释放时释放信号量的可释放对象，从而将此信号量视作"多锁"使用。
    /// </summary>
    public IDisposable Lock() => Lock(CancellationToken.None);

    // ReSharper disable UnusedMember.Local
    [DebuggerNonUserCode]
    private sealed class DebugView
    {
        private readonly AsyncSemaphore _semaphore;

        public DebugView(AsyncSemaphore semaphore)
        {
            _semaphore = semaphore;
        }

        public int Id => _semaphore.Id;

        public long CurrentCount => _semaphore._count;

        public IAsyncWaitQueue<object> WaitQueue => _semaphore._queue;
    }
}
