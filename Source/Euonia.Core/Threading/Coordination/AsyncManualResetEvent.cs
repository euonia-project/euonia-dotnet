using System.Diagnostics;
using Nerosoft.Euonia.Threading.Interop;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的手动重置事件。
/// <seealso href="http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266920.aspx">原始想法来自 Stephen Toub</seealso>
/// </summary>
[DebuggerDisplay("Id = {Id}, IsSet = {GetStateForDebugger}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncManualResetEvent
{
    /// <summary>
    /// 用于同步的对象。
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// 事件的当前状态。
    /// </summary>
    private TaskCompletionSource<object> _taskCompletionSource;

    /// <summary>
    /// 此实例的半唯一标识符。如果尚未创建 ID，则为 0。
    /// </summary>
    private int _id;

    [DebuggerNonUserCode]
    private bool GetStateForDebugger
    {
        get
        {
            lock (_mutex)
            {
                return _taskCompletionSource.Task.IsCompleted;
            }
        }
    }

    /// <summary>
    /// 创建一个异步兼容的手动重置事件。
    /// </summary>
    /// <param name="set">手动重置事件初始是否处于设置状态。</param>
    public AsyncManualResetEvent(bool set)
    {
        _mutex = new object();
        _taskCompletionSource = Extensions.CreateAsyncTaskSource<object>();
        if (set)
            _taskCompletionSource.TrySetResult(null);
    }

    /// <summary>
    /// 创建一个初始状态为未设置的异步兼容的手动重置事件。
    /// </summary>
    public AsyncManualResetEvent()
        : this(false)
    {
    }

    /// <summary>
    /// 获取此异步手动重置事件的半唯一标识符。
    /// </summary>
    public int Id => IdentifierManager<AsyncManualResetEvent>.GetId(ref _id);

    /// <summary>
    /// 此事件当前是否处于设置状态。此成员很少使用；使用此成员的代码很可能存在竞态条件。
    /// </summary>
    public bool IsSet
    {
        get
        {
            lock (_mutex)
            {
                return _taskCompletionSource.Task.IsCompleted;
            }
        }
    }

    /// <summary>
    /// 异步等待此事件被设置。
    /// </summary>
    public Task WaitAsync()
    {
        lock (_mutex)
        {
            return _taskCompletionSource.Task;
        }
    }

    /// <summary>
    /// 异步等待此事件被设置或等待被取消。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果此令牌已被取消，此方法将首先检查事件是否已被设置。</param>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        var waitTask = WaitAsync(cancellationToken);
        return waitTask.IsCompleted ? waitTask : waitTask.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// 同步等待此事件被设置。此方法可能会阻塞调用线程。
    /// </summary>
    public void Wait()
    {
        WaitAsync().WaitAndUnwrapException();
    }

    /// <summary>
    /// 同步等待此事件被设置。此方法可能会阻塞调用线程。
    /// </summary>
    /// <param name="cancellationToken">用于取消等待的取消令牌。如果此令牌已被取消，此方法将首先检查事件是否已被设置。</param>
    public void Wait(CancellationToken cancellationToken)
    {
        var ret = WaitAsync(cancellationToken);
        if (ret.IsCompleted)
            return;
        ret.WaitAndUnwrapException(cancellationToken);
    }

    /// <summary>
    /// 设置事件，原子性地完成 <see cref="O:Nito.AsyncEx.AsyncManualResetEvent.WaitAsync"/> 返回的每个任务。如果事件已经设置，此方法不执行任何操作。
    /// </summary>
    public void Set()
    {
        lock (_mutex)
        {
            _taskCompletionSource.TrySetResult(null);
        }
    }

    /// <summary>
    /// 重置事件。如果事件已经重置，此方法不执行任何操作。
    /// </summary>
    public void Reset()
    {
        lock (_mutex)
        {
            if (_taskCompletionSource.Task.IsCompleted)
            {
                _taskCompletionSource = Extensions.CreateAsyncTaskSource<object>();
            }
        }
    }

    // ReSharper disable UnusedMember.Local
    [DebuggerNonUserCode]
    private sealed class DebugView
    {
        private readonly AsyncManualResetEvent _mre;

        public DebugView(AsyncManualResetEvent mre)
        {
            _mre = mre;
        }

        public int Id => _mre.Id;

        public bool IsSet => _mre.GetStateForDebugger;

        public Task CurrentTask => _mre._taskCompletionSource.Task;
    }
    // ReSharper restore UnusedMember.Local
}
