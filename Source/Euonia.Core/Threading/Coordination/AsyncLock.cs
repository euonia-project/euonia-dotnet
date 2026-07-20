// 原始想法来自 Stephen Toub: http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx

using System.Diagnostics;
using Nerosoft.Euonia.Disposing;
using Nerosoft.Euonia.Threading.Interop;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个与异步兼容的互斥锁。请注意，此锁<b>不</b>可重入！
/// </summary>
/// <remarks>
/// <para>这是 <c>lock</c> 关键字或 <see cref="Mutex"/> 类型与 <c>async</c> 兼容的近似等价物，类似于 <a href="http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx">Stephen Toub 的 AsyncLock</a>。它之所以只是<i>近似</i>等价，是因为 <c>lock</c> 关键字允许重入，而目前无法用与 <c>async</c> 兼容的锁实现这一点。</para>
/// <para><see cref="AsyncLock"/> 要么被持有，要么未持有。可以通过调用 <see autoUpgrade="true" cref="LockAsync()"/> 异步获取锁，并通过释放该任务的结果来释放锁。<see cref="LockAsync(CancellationToken)"/> 接受一个可选的 <see cref="CancellationToken"/>，可用于取消锁的获取。</para>
/// <para>从 <see autoUpgrade="true" cref="LockAsync()"/> 返回的任务在获取到 <see cref="AsyncLock"/> 后将进入 <c>Completed</c> 状态。如果 <see cref="CancellationToken"/> 在等待完成之前被发出信号，同样的任务将进入 <c>Canceled</c> 状态；在这种情况下，该任务不会获取到 <see cref="AsyncLock"/>。</para>
/// <para>你可以使用已取消的 <see cref="CancellationToken"/> 调用 <see cref="Lock(CancellationToken)"/> 或 <see cref="LockAsync(CancellationToken)"/>，以尝试在不进入等待队列的情况下立即获取 <see cref="AsyncLock"/>。</para>
/// </remarks>
/// <example>
/// <para>绝大多数用例是直接替换 <c>lock</c> 语句。也就是说，原始代码如下所示：</para>
/// <code>
/// private readonly object _mutex = new object();
/// public void DoStuff()
/// {
///     lock (_mutex)
///     {
///         Thread.Sleep(TimeSpan.FromSeconds(1));
///     }
/// }
/// </code>
/// <para>如果我们想将阻塞操作 <c>Thread.Sleep</c> 替换为异步等效操作，由于 <c>lock</c> 块的存在，这不能直接实现。我们不能在 <c>lock</c> 内部 <c>await</c>。</para>
/// <para>因此，我们改用与 <c>async</c> 兼容的 <see cref="AsyncLock"/>：</para>
/// <code>
/// private readonly AsyncLock _mutex = new AsyncLock();
/// public async Task DoStuffAsync()
/// {
///     using (await _mutex.LockAsync())
///     {
///         await Task.Delay(TimeSpan.FromSeconds(1));
///     }
/// }
/// </code>
/// </example>
[DebuggerDisplay("Id = {Id}, Taken = {_taken}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncLock
{
    /// <summary>
    /// 锁是否被某个任务持有。
    /// </summary>
    private bool _taken;

    /// <summary>
    /// 其他任务正在等待获取锁的 TCS 队列。
    /// </summary>
    private readonly IAsyncWaitQueue<IDisposable> _queue;

    /// <summary>
    /// 此实例的半唯一标识符。如果尚未创建 ID，则为 0。
    /// </summary>
    private int _id;

    /// <summary>
    /// 用于互斥的对象。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 创建一个新的异步兼容互斥锁。
    /// </summary>
    public AsyncLock()
        :this(null)
    {
    }

    /// <summary>
    /// 使用指定的等待队列创建一个新的异步兼容互斥锁。
    /// </summary>
    /// <param name="queue">用于管理等待者的等待队列。可以为 <c>null</c> 以使用默认（FIFO）队列。</param>
    internal AsyncLock(IAsyncWaitQueue<IDisposable> queue)
    {
        _queue = queue ?? new DefaultAsyncWaitQueue<IDisposable>();
        _mutex = new object();
    }

    /// <summary>
    /// 获取此异步锁的半唯一标识符。
    /// </summary>
    public int Id => IdentifierManager<AsyncLock>.GetId(ref _id);

    /// <summary>
    /// 异步获取锁。返回一个在释放时解除锁定的可释放对象。
    /// </summary>
    /// <param name="cancellationToken">用于取消锁的取消令牌。如果已设置，此方法将尝试立即获取锁（如果锁当前可用则成功）。</param>
    /// <returns>一个在释放时解除锁定的可释放对象。</returns>
    private Task<IDisposable> RequestLockAsync(CancellationToken cancellationToken)
    {
        lock (_mutex)
        {
            if (!_taken)
            {
                // 如果锁可用，立即获取。
                _taken = true;
                return Task.FromResult<IDisposable>(new Key(this));
            }
            else
            {
                // 等待锁变为可用或取消。
                return _queue.Enqueue(_mutex, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 异步获取锁。返回一个在释放时解除锁定的可释放对象。
    /// </summary>
    /// <param name="cancellationToken">用于取消锁的取消令牌。如果已设置，此方法将尝试立即获取锁（如果锁当前可用则成功）。</param>
    /// <returns>一个在释放时解除锁定的可释放对象。</returns>
    public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken)
    {
        return new AwaitableDisposable<IDisposable>(RequestLockAsync(cancellationToken));
    }

    /// <summary>
    /// 异步获取锁。返回一个在释放时解除锁定的可释放对象。
    /// </summary>
    /// <returns>一个在释放时解除锁定的可释放对象。</returns>
    public AwaitableDisposable<IDisposable> LockAsync()
    {
        return LockAsync(CancellationToken.None);
    }

    /// <summary>
    /// 同步获取锁。返回一个在释放时解除锁定的可释放对象。此方法可能会阻塞调用线程。
    /// </summary>
    /// <param name="cancellationToken">用于取消锁的取消令牌。如果已设置，此方法将尝试立即获取锁（如果锁当前可用则成功）。</param>
    public IDisposable Lock(CancellationToken cancellationToken)
    {
        return RequestLockAsync(cancellationToken).WaitAndUnwrapException();
    }

    /// <summary>
    /// 同步获取锁。返回一个在释放时解除锁定的可释放对象。此方法可能会阻塞调用线程。
    /// </summary>
    public IDisposable Lock()
    {
        return Lock(CancellationToken.None);
    }

    /// <summary>
    /// 释放锁。
    /// </summary>
    internal void ReleaseLock()
    {
        lock (_mutex)
        {
            if (_queue.IsEmpty)
                _taken = false;
            else
                _queue.Dequeue(new Key(this));
        }
    }

    /// <summary>
    /// 释放锁的可释放对象。
    /// </summary>
    private sealed class Key : SingleDisposable<AsyncLock>
    {
        /// <summary>
        /// 为锁创建密钥。
        /// </summary>
        /// <param name="asyncLock">要释放的锁。不能为 <c>null</c>。</param>
        public Key(AsyncLock asyncLock)
            : base(asyncLock)
        {
        }

        protected override void Dispose(AsyncLock context)
        {
            context.ReleaseLock();
        }
    }

    // ReSharper disable UnusedMember.Local
    [DebuggerNonUserCode]
    private sealed class DebugView
    {
        private readonly AsyncLock _mutex;

        public DebugView(AsyncLock mutex)
        {
            _mutex = mutex;
        }

        public int Id => _mutex.Id;

        public bool Taken => _mutex._taken;

        public IAsyncWaitQueue<IDisposable> WaitQueue => _mutex._queue;
    }
    // ReSharper restore UnusedMember.Local
}
