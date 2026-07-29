using System.Diagnostics;
using Nerosoft.Euonia.Disposing;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 在 <see cref="AsyncContext"/> 中执行操作的线程。
/// </summary>
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncContextThread : SingleDisposable<AsyncContext>
{
    /// <summary>
    /// 子线程。
    /// </summary>
    private readonly Task _thread;

    /// <summary>
    /// 创建一个新的 <see cref="AsyncContext"/> 并增加其操作计数。
    /// </summary>
    private static AsyncContext CreateAsyncContext()
    {
        var result = new AsyncContext();
        result.SynchronizationContext.OperationStarted();
        return result;
    }

    /// <summary>
    /// 初始化 <see cref="AsyncContextThread"/> 类的新实例，创建一个等待命令的子线程。
    /// </summary>
    /// <param name="context">此线程的上下文。</param>
    private AsyncContextThread(AsyncContext context)
        : base(context)
    {
        Context = context;
        _thread = Task.Factory.StartNew(Execute, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>
    /// 初始化 <see cref="AsyncContextThread"/> 类的新实例，创建一个等待命令的子线程。
    /// </summary>
    public AsyncContextThread()
        : this(CreateAsyncContext())
    {
    }

    /// <summary>
    /// 获取由此线程执行的 <see cref="AsyncContext"/>。
    /// </summary>
    public AsyncContext Context { get; }

    private void Execute()
    {
        using (Context)
        {
            Context.Execute();
        }
    }

    /// <summary>
    /// 允许线程退出（如果尚未退出）。
    /// </summary>
    private void AllowThreadToExit()
    {
        Context.SynchronizationContext.OperationCompleted();
    }

    /// <summary>
    /// 请求线程退出并返回一个表示线程退出的任务。线程将在所有未完成的异步操作完成后退出。
    /// </summary>
    public Task JoinAsync()
    {
        Dispose();
        return _thread;
    }

    /// <summary>
    /// 请求线程退出并阻塞直到线程退出。线程将在所有未完成的异步操作完成后退出。
    /// </summary>
    public void Join()
    {
        JoinAsync().WaitAndUnwrapException();
    }

    /// <summary>
    /// 请求线程退出。
    /// </summary>
    protected override void Dispose(AsyncContext context)
    {
        AllowThreadToExit();
    }

    /// <summary>
    /// 获取此线程的 <see cref="TaskFactory"/>，可用于将工作调度到此线程。
    /// </summary>
    public TaskFactory Factory => Context.Factory;

    [DebuggerNonUserCode]
    internal sealed class DebugView
    {
        private readonly AsyncContextThread _thread;

        public DebugView(AsyncContextThread thread)
        {
            _thread = thread;
        }

        public AsyncContext Context => _thread.Context;

        public object Thread => _thread._thread;
    }
}