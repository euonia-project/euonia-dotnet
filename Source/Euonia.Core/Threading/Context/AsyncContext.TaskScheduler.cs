namespace Nerosoft.Euonia.Threading;

public sealed partial class AsyncContext
{
    /// <summary>
    /// 一个任务调度器，用于将任务调度到异步上下文。
    /// </summary>
    private sealed class AsyncContextTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// 此任务调度器的异步上下文。
        /// </summary>
        private readonly AsyncContext _context;

        /// <summary>
        /// 初始化 <see cref="AsyncContextTaskScheduler"/> 类的新实例。
        /// </summary>
        /// <param name="context">此任务调度器的异步上下文。不能为 <c>null</c>。</param>
        public AsyncContextTaskScheduler(AsyncContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 生成当前排队等待调度器执行的 <see cref="T:System.Threading.Tasks.Task"/> 实例的枚举。
        /// </summary>
        /// <returns>允许遍历当前排队等待此调度器的任务的枚举。</returns>
        [System.Diagnostics.DebuggerNonUserCode]
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _context._queue.GetScheduledTasks();
        }

        /// <summary>
        /// 将 <see cref="T:System.Threading.Tasks.Task"/> 排队到调度器中。如果所有任务已完成且未完成的异步操作计数为零，则此方法的行为是未定义的。
        /// </summary>
        /// <param name="task">要排队的 <see cref="T:System.Threading.Tasks.Task"/>。</param>
        protected override void QueueTask(Task task)
        {
            _context.Enqueue(task, false);
        }

        /// <summary>
        /// 确定提供的 <see cref="T:System.Threading.Tasks.Task"/> 是否可以在此调用中同步执行，如果可以，则执行它。
        /// </summary>
        /// <param name="task">要执行的 <see cref="T:System.Threading.Tasks.Task"/>。</param>
        /// <param name="taskWasPreviouslyQueued">一个布尔值，表示任务是否先前已排队。如果此参数为 True，则任务可能先前已排队（已调度）；如果为 False，则已知任务尚未排队，此调用是为了在不排队的情况下内联执行任务。</param>
        /// <returns>一个布尔值，指示任务是否已内联执行。</returns>
        /// <exception cref="T:System.InvalidOperationException"><paramref name="task"/> 已经被执行。</exception>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return (AsyncContext.Current == _context) && TryExecuteTask(task);
        }

        /// <summary>
        /// 指示此 <see cref="T:System.Threading.Tasks.TaskScheduler"/> 能够支持的最大并发级别。
        /// </summary>
        public override int MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        /// <summary>
        /// 公开基类的 <see cref="TaskScheduler.TryExecuteTask"/> 方法。
        /// </summary>
        /// <param name="task">要尝试执行的任务。</param>
        public void DoTryExecuteTask(Task task)
        {
            TryExecuteTask(task);
        }
    }
}