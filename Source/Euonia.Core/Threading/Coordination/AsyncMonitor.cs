using System.Diagnostics;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个异步兼容的监视器。
/// </summary>
[DebuggerDisplay("Id = {Id}, ConditionVariableId = {_conditionVariable.Id}")]
public sealed class AsyncMonitor
{
    /// <summary>
    /// 锁。
    /// </summary>
    private readonly AsyncLock _asyncLock;

    /// <summary>
    /// 条件变量。
    /// </summary>
    private readonly AsyncConditionVariable _conditionVariable;

    /// <summary>
    /// 构造一个新的监视器。
    /// </summary>
    /// <param name="lockQueue">用于管理锁等待者的等待队列。可以为 <c>null</c> 以使用默认（FIFO）队列。</param>
    /// <param name="conditionVariableQueue">用于管理信号等待者的等待队列。可以为 <c>null</c> 以使用默认（FIFO）队列。</param>
    internal AsyncMonitor(IAsyncWaitQueue<IDisposable> lockQueue, IAsyncWaitQueue<object> conditionVariableQueue)
    {
        _asyncLock = new AsyncLock(lockQueue);
        _conditionVariable = new AsyncConditionVariable(_asyncLock, conditionVariableQueue);
    }

    /// <summary>
    /// 构造一个新的监视器。
    /// </summary>
    public AsyncMonitor()
        : this(null, null)
    {
    }

    /// <summary>
    /// 获取此监视器的半唯一标识符。
    /// </summary>
    public int Id => _asyncLock.Id;

    /// <summary>
    /// 异步进入监视器。返回一个在释放时离开监视器的可释放对象。
    /// </summary>
    /// <param name="cancellationToken">用于取消进入的取消令牌。如果已设置，此方法将尝试立即进入监视器（如果监视器当前可用则成功）。</param>
    /// <returns>一个在释放时离开监视器的可释放对象。</returns>
    public AwaitableDisposable<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        return _asyncLock.LockAsync(cancellationToken);
    }

    /// <summary>
    /// 异步进入监视器。返回一个在释放时离开监视器的可释放对象。
    /// </summary>
    /// <returns>一个在释放时离开监视器的可释放对象。</returns>
    public AwaitableDisposable<IDisposable> EnterAsync()
    {
        return EnterAsync(CancellationToken.None);
    }

    /// <summary>
    /// 同步进入监视器。返回一个在释放时离开监视器的可释放对象。此方法可能会阻塞调用线程。
    /// </summary>
    /// <param name="cancellationToken">用于取消进入的取消令牌。如果已设置，此方法将尝试立即进入监视器（如果监视器当前可用则成功）。</param>
    public IDisposable Enter(CancellationToken cancellationToken)
    {
        return _asyncLock.Lock(cancellationToken);
    }

    /// <summary>
    /// 同步进入监视器。返回一个在释放时离开监视器的可释放对象。此方法可能会阻塞调用线程。
    /// </summary>
    public IDisposable Enter()
    {
        return Enter(CancellationToken.None);
    }

    /// <summary>
    /// 异步等待此监视器上的脉冲信号。调用此方法时监视器必须已经进入，并且在此方法返回时监视器仍将处于进入状态，即使该方法被取消。此方法在内部会在等待通知期间离开监视器。
    /// </summary>
    /// <param name="cancellationToken">用于取消此等待的取消信号。</param>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        return _conditionVariable.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// 异步等待此监视器上的脉冲信号。调用此方法时监视器必须已经进入，并且在此方法返回时监视器仍将处于进入状态。此方法在内部会在等待通知期间离开监视器。
    /// </summary>
    public Task WaitAsync()
    {
        return WaitAsync(CancellationToken.None);
    }

    /// <summary>
    /// 同步等待此监视器上的脉冲信号。此方法可能会阻塞调用线程。调用此方法时监视器必须已经进入，并且在此方法返回时监视器仍将处于进入状态，即使该方法被取消。此方法在内部会在等待通知期间离开监视器。
    /// </summary>
    /// <param name="cancellationToken">用于取消此等待的取消信号。</param>
    public void Wait(CancellationToken cancellationToken)
    {
        _conditionVariable.Wait(cancellationToken);
    }

    /// <summary>
    /// 同步等待此监视器上的脉冲信号。此方法可能会阻塞调用线程。调用此方法时监视器必须已经进入，并且在此方法返回时监视器仍将处于进入状态。此方法在内部会在等待通知期间离开监视器。
    /// </summary>
    public void Wait()
    {
        Wait(CancellationToken.None);
    }

    /// <summary>
    /// 向正在等待此监视器的单个任务发送信号。调用此方法时监视器必须已经进入，并且在此方法返回时监视器仍将处于进入状态。
    /// </summary>
    public void Pulse()
    {
        _conditionVariable.Notify();
    }

    /// <summary>
    /// 向正在等待此监视器的所有任务发送信号。调用此方法时监视器必须已经进入，并且在此方法返回时监视器仍将处于进入状态。
    /// </summary>
    public void PulseAll()
    {
        _conditionVariable.NotifyAll();
    }
}
