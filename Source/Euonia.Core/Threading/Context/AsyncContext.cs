using System.ComponentModel;
using System.Diagnostics;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 为异步操作提供上下文。此类是线程安全的。
/// </summary>
/// <remarks>
/// <para><see cref="Execute()"/> 只能调用一次。在 <see cref="Execute()"/> 返回后，应释放异步上下文。</para>
/// </remarks>
[DebuggerDisplay("Id = {Id}, OperationCount = {_outstandingOperations}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed partial class AsyncContext : IDisposable
{
    /// <summary>
    /// 存放要运行的操作的队列。
    /// </summary>
    private readonly TaskQueue _queue;

    /// <summary>
    /// 此 <see cref="AsyncContext"/> 的 <see cref="SynchronizationContext"/>。
    /// </summary>
    private readonly AsyncContextSynchronizationContext _synchronizationContext;

    /// <summary>
    /// 此 <see cref="AsyncContext"/> 的 <see cref="TaskScheduler"/>。
    /// </summary>
    private readonly AsyncContextTaskScheduler _taskScheduler;

    /// <summary>
    /// 此 <see cref="AsyncContext"/> 的 <see cref="TaskFactory"/>。
    /// </summary>
    private readonly TaskFactory _taskFactory;

    /// <summary>
    /// 未完成操作的数量，包括队列中的操作。
    /// </summary>
    private int _outstandingOperations;

    /// <summary>
    /// 初始化 <see cref="AsyncContext"/> 类的新实例。这是一个高级操作；大多数用户应该改用静态的 <c>Run</c> 方法。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public AsyncContext()
    {
        _queue = new TaskQueue();
        _synchronizationContext = new AsyncContextSynchronizationContext(this);
        _taskScheduler = new AsyncContextTaskScheduler(this);
        _taskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.HideScheduler, _taskScheduler);
    }

    /// <summary>
    /// 获取此异步上下文的半唯一标识符。此标识符与上下文的 <see cref="TaskScheduler"/> 的标识符相同。
    /// </summary>
    public int Id => _taskScheduler.Id;

    /// <summary>
    /// 增加未完成的异步操作计数。
    /// </summary>
    private void OperationStarted()
    {
        var _ = Interlocked.Increment(ref _outstandingOperations);
    }

    /// <summary>
    /// 减少未完成的异步操作计数。
    /// </summary>
    private void OperationCompleted()
    {
        var newCount = Interlocked.Decrement(ref _outstandingOperations);
        if (newCount == 0)
            _queue.CompleteAdding();
    }

    /// <summary>
    /// 将任务排队以待 <see cref="Execute"/> 执行。如果所有任务已完成且未完成的异步操作计数为零，则此方法的行为是未定义的。
    /// </summary>
    /// <param name="task">要排队的任务。不能为 <c>null</c>。</param>
    /// <param name="propagateExceptions">一个值，指示此任务上的异常是否应传播到主循环之外。</param>
    private void Enqueue(Task task, bool propagateExceptions)
    {
        OperationStarted();
        task.ContinueWith(_ => OperationCompleted(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, _taskScheduler);
        _queue.TryAdd(task, propagateExceptions);

        // 如果添加到队列失败，则直接丢弃该 Task。这与 TaskScheduler.FromCurrentSynchronizationContext(WinFormsSynchronizationContext) 的行为相同。
    }

    /// <summary>
    /// 释放此类使用的所有资源。不应在 <see cref="Execute"/> 正在执行时调用此方法。
    /// </summary>
    public void Dispose()
    {
        _queue.Dispose();
    }

    /// <summary>
    /// 执行所有排队的操作。当所有任务已完成且未完成的异步操作计数为零时，此方法返回。此方法将解包并传播应传播错误的任务中的错误。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Execute()
    {
        SynchronizationContextSwitcher.ApplyContext(_synchronizationContext, () =>
        {
            var tasks = _queue.GetConsumingEnumerable();
            foreach (var task in tasks)
            {
                _taskScheduler.DoTryExecuteTask(task.Item1);

                // 如有必要，传播异常。
                if (task.Item2)
                    task.Item1.WaitAndUnwrapException();
            }
        });
    }

    /// <summary>
    /// 将任务排队执行，并开始执行队列中的所有任务。当所有任务已完成且未完成的异步操作计数为零时，此方法返回。此方法将解包并传播任务中的错误。
    /// </summary>
    /// <param name="action">要执行的操作。不能为 <c>null</c>。</param>
    public static void Run(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        using (var context = new AsyncContext())
        {
            var task = context._taskFactory.Run(action);
            context.Execute();
            task.WaitAndUnwrapException();
        }
    }

    /// <summary>
    /// 将任务排队执行，并开始执行队列中的所有任务。当所有任务已完成且未完成的异步操作计数为零时，此方法返回。返回任务的结果。此方法将解包并传播任务中的错误。
    /// </summary>
    /// <typeparam name="TResult">任务的结果类型。</typeparam>
    /// <param name="action">要执行的操作。不能为 <c>null</c>。</param>
    public static TResult Run<TResult>(Func<TResult> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        using (var context = new AsyncContext())
        {
            var task = context._taskFactory.Run(action);
            context.Execute();
            return task.WaitAndUnwrapException();
        }
    }

    /// <summary>
    /// 将任务排队执行，并开始执行队列中的所有任务。当所有任务已完成且未完成的异步操作计数为零时，此方法返回。此方法将解包并传播任务代理中的错误。
    /// </summary>
    /// <param name="action">要执行的操作。不能为 <c>null</c>。</param>
    public static void Run(Func<Task> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // ReSharper disable AccessToDisposedClosure
        using (var context = new AsyncContext())
        {
            context.OperationStarted();
            var task = context._taskFactory.Run(action).ContinueWith(t =>
            {
                context.OperationCompleted();
                t.WaitAndUnwrapException();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context._taskScheduler);
            context.Execute();
            task.WaitAndUnwrapException();
        }
        // ReSharper restore AccessToDisposedClosure
    }

    /// <summary>
    /// 将任务排队执行，并开始执行队列中的所有任务。当所有任务已完成且未完成的异步操作计数为零时，此方法返回。返回任务代理的结果。此方法将解包并传播任务代理中的错误。
    /// </summary>
    /// <typeparam name="TResult">任务的结果类型。</typeparam>
    /// <param name="action">要执行的操作。不能为 <c>null</c>。</param>
    public static TResult Run<TResult>(Func<Task<TResult>> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // ReSharper disable AccessToDisposedClosure
        using (var context = new AsyncContext())
        {
            context.OperationStarted();
            var task = context._taskFactory.Run(action).ContinueWith(t =>
            {
                context.OperationCompleted();
                return t.WaitAndUnwrapException();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context._taskScheduler);
            context.Execute();
            return task.WaitAndUnwrapException();
        }
        // ReSharper restore AccessToDisposedClosure
    }

    // ReSharper disable once MemberCanBePrivate.Global

    /// <summary>
    /// 获取当前线程的 <see cref="AsyncContext"/>，如果此线程当前未在 <see cref="AsyncContext"/> 中运行，则返回 <c>null</c>。
    /// </summary>
    public static AsyncContext Current
    {
        get
        {
            var syncContext = SynchronizationContext.Current as AsyncContextSynchronizationContext;
            return syncContext?.Context;
        }
    }

    /// <summary>
    /// 获取此 <see cref="AsyncContext"/> 的 <see cref="SynchronizationContext"/>。在 <see cref="Execute"/> 内部，此值始终等于 <see cref="SynchronizationContext"/>。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SynchronizationContext SynchronizationContext => _synchronizationContext;

    /// <summary>
    /// 获取此 <see cref="AsyncContext"/> 的 <see cref="TaskScheduler"/>。在 <see cref="Execute"/> 内部，此值始终等于 <see cref="TaskScheduler.Current"/>。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TaskScheduler Scheduler => _taskScheduler;

    /// <summary>
    /// 获取此 <see cref="AsyncContext"/> 的 <see cref="TaskFactory"/>。请注意，此工厂已设置 <see cref="TaskCreationOptions.HideScheduler"/> 选项。使用异步委托时请小心；您可能需要调用 <see cref="M:System.Threading.SynchronizationContext.OperationStarted"/> 和 <see cref="M:System.Threading.SynchronizationContext.OperationCompleted"/> 以防止此 <see cref="AsyncContext"/> 过早终止。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TaskFactory Factory => _taskFactory;

    [DebuggerNonUserCode]
    internal sealed class DebugView
    {
        private readonly AsyncContext _context;

        public DebugView(AsyncContext context)
        {
            _context = context;
        }

        public TaskScheduler TaskScheduler => _context._taskScheduler;
    }
}