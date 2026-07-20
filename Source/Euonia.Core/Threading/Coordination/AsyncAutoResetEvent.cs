using System.Diagnostics;
using Nerosoft.Euonia.Threading.Interop;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的自动重置事件。
/// </summary>
[DebuggerDisplay("Id = {Id}, IsSet = {_set}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncAutoResetEvent
{
    /// <summary>
    /// 其他任务正在等待的 TCS 队列。
    /// </summary>
    private readonly IAsyncWaitQueue<object> _queue;

    /// <summary>
    /// 事件的当前状态。
    /// </summary>
    private bool _set;

    /// <summary>
    /// 此实例的半唯一标识符。如果尚未创建 ID，则为 0。
    /// </summary>
    private int _id;

    /// <summary>
    /// 用于互斥的对象。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 创建一个异步兼容的自动重置事件。
    /// </summary>
    /// <param name="set">自动重置事件初始是否处于设置状态。</param>
    /// <param name="queue">用于管理等待者的等待队列。可以为 <c>null</c> 以使用默认（FIFO）队列。</param>
    internal AsyncAutoResetEvent(bool set, IAsyncWaitQueue<object> queue)
    {
        _queue = queue ?? new DefaultAsyncWaitQueue<object>();
        _set = set;
        _mutex = new object();
    }

    /// <summary>
    /// 创建一个异步兼容的自动重置事件。
    /// </summary>
    /// <param name="set">自动重置事件初始是否处于设置状态。</param>
    public AsyncAutoResetEvent(bool set)
        : this(set, null)
    {
    }

    /// <summary>
    /// 创建一个初始状态为未设置的异步兼容的自动重置事件。
    /// </summary>
    public AsyncAutoResetEvent()
        : this(false, null)
    {
    }

    /// <summary>
    /// 获取此异步自动重置事件的半唯一标识符。
    /// </summary>
    public int Id => IdentifierManager<AsyncAutoResetEvent>.GetId(ref _id);

    /// <summary>
    /// 此事件当前是否处于设置状态。此成员很少使用；使用此成员的代码很可能存在竞态条件。
    /// </summary>
    public bool IsSet
    {
        get { lock (_mutex) return _set; }
    }

    /// <summary>
    /// 异步等待此事件被设置。如果事件已被设置，此方法将自动重置它并立即返回，即使取消令牌已经被发出信号。如果等待被取消，则不会自动重置此事件。
    /// </summary>
    /// <param name="cancellationToken">用于取消此等待的取消令牌。</param>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        Task ret;
        lock (_mutex)
        {
            if (_set)
            {
                _set = false;
                ret = TaskConstants.Completed;
            }
            else
            {
                ret = _queue.Enqueue(_mutex, cancellationToken);
            }
        }

        return ret;
    }

    /// <summary>
    /// 异步等待此事件被设置。如果事件已被设置，此方法将自动重置它并立即返回。
    /// </summary>
    public Task WaitAsync()
    {
        return WaitAsync(CancellationToken.None);
    }

    /// <summary>
    /// 同步等待此事件被设置。如果事件已被设置，此方法将自动重置它并立即返回，即使取消令牌已经被发出信号。如果等待被取消，则不会自动重置此事件。此方法可能会阻塞调用线程。
    /// </summary>
    /// <param name="cancellationToken">用于取消此等待的取消令牌。</param>
    public void Wait(CancellationToken cancellationToken)
    {
        WaitAsync(cancellationToken).WaitAndUnwrapException(cancellationToken);
    }

    /// <summary>
    /// 同步等待此事件被设置。如果事件已被设置，此方法将自动重置它并立即返回。此方法可能会阻塞调用线程。
    /// </summary>
    public void Wait()
    {
        Wait(CancellationToken.None);
    }

    /// <summary>
    /// 设置事件，原子性地完成 <see cref="o:WaitAsync"/> 返回的任务。如果事件已经设置，此方法不执行任何操作。
    /// </summary>
    public void Set()
    {
        lock (_mutex)
        {
            if (_queue.IsEmpty)
                _set = true;
            else
                _queue.Dequeue();
        }
    }

    // ReSharper disable UnusedMember.Local
    [DebuggerNonUserCode]
    private sealed class DebugView
    {
        private readonly AsyncAutoResetEvent _are;

        public DebugView(AsyncAutoResetEvent are)
        {
            _are = are;
        }

        public int Id => _are.Id;

        public bool IsSet => _are._set;

        public IAsyncWaitQueue<object> WaitQueue => _are._queue;
    }
    // ReSharper restore UnusedMember.Local
}
