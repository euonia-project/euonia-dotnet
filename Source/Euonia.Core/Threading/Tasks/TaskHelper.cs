namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 用于操作任务的辅助方法。
/// </summary>
public static class TaskHelper
{
    /// <summary>
    /// 同步执行委托，并将其结果封装在任务中。返回的任务已经完成。
    /// </summary>
    /// <param name="func">要同步执行的委托。</param>
#pragma warning disable 1998
    public static async Task ExecuteAsTask(Action func)
#pragma warning restore 1998
    {
        func();
    }

    /// <summary>
    /// 同步执行委托，并将其结果封装在任务中。返回的任务已经完成。
    /// </summary>
    /// <param name="func">要同步执行的委托。</param>
#pragma warning disable 1998
    public static async Task<T> ExecuteAsTask<T>(Func<T> func)
#pragma warning restore 1998
    {
        return func();
    }

    /// <summary>
    /// 获取一个值，指示当前线程是否正在同步运行。
    /// </summary>
    [field: ThreadStatic]
    public static bool IsSynchronous { get; private set; }

    /// <summary>
    /// 同步运行 <paramref name="action"/>。
    /// </summary>
    public static void Run<TState>(Func<TState, ValueTask> action, TState state)
    {
        Run(
            async s =>
            {
                await s.action(s.state).ConfigureAwait(false);
                return true;
            },
            (action, state)
        );
    }

    /// <summary>
    /// 同步运行 <paramref name="action"/>。
    /// </summary>
    public static TResult Run<TState, TResult>(Func<TState, ValueTask<TResult>> action, TState state)
    {
        Invariant.Require(!IsSynchronous);

        try
        {
            IsSynchronous = true;

            var task = action(state);
            Invariant.Require(task.IsCompleted);

            // 这几乎不会发生（在调试构建中不可能发生）。然而，为了在发布构建中绝对确保，我们将其作为回退逻辑。
            // this should never happen (and can't in the debug build). However, to make absolutely sure we have this as
            // fallback logic for the release build
            if (!task.IsCompleted)
            {
                // 调用 AsTask()，因为 https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.1
                // 指出我们不应该在未完成的 ValueTask 上调用 GetAwaiter().GetResult()。
                // call AsTask(), since https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.1
                // says that we should not call GetAwaiter().GetResult() except on a completed ValueTask
                return task.AsTask().GetAwaiter().GetResult();
            }

            return task.GetAwaiter().GetResult();
        }
        finally
        {
            IsSynchronous = false;
        }
    }

    /// <summary>
    /// 提供与 <see cref="TaskHelper"/> 兼容的 <see cref="Task.Delay(TimeSpan, CancellationToken)"/> 实现。
    /// </summary>
    public static ValueTask Delay(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        if (!IsSynchronous)
        {
            return Task.Delay(timeout.InMilliseconds, cancellationToken).AsValueTask();
        }

        if (cancellationToken.CanBeCanceled)
        {
            if (cancellationToken.WaitHandle.WaitOne(timeout.InMilliseconds))
            {
                throw new OperationCanceledException("delay was canceled", cancellationToken);
            }
        }
        else
        {
            Thread.Sleep(timeout.InMilliseconds);
        }

        return default;
    }

    /// <summary>
    /// 对于同时实现了 <see cref="IAsyncDisposable"/> 和 <see cref="IDisposable"/> 的类型 <typeparamref name="TDisposable"/>，
    /// 使用 <see cref="IAsyncDisposable.DisposeAsync"/> 提供 <see cref="IDisposable.Dispose"/> 的实现。
    /// </summary>
    public static void DisposeSyncViaAsync<TDisposable>(this TDisposable disposable)
        where TDisposable : IAsyncDisposable, IDisposable =>
        Run(@this => @this.DisposeAsync(), disposable);

    /// <summary>
    /// 在同步模式下，对提供的 <paramref name="task"/> 执行阻塞等待。在异步模式下，
    /// 将 <paramref name="task"/> 作为 <see cref="ValueTask{TResult}"/> 返回。
    /// </summary>
    public static ValueTask<TResult> AwaitSyncOverAsync<TResult>(this Task<TResult> task) =>
        IsSynchronous ? task.GetAwaiter().GetResult().AsValueTask() : task.AsValueTask();

    /// <summary>
    /// 在同步模式下，对提供的 <paramref name="task"/> 执行阻塞等待。在异步模式下，
    /// 将 <paramref name="task"/> 作为 <see cref="ValueTask"/> 返回。
    /// </summary>
    public static ValueTask AwaitSyncOverAsync(this Task task)
    {
        if (IsSynchronous)
        {
            task.GetAwaiter().GetResult();
            return default;
        }

        return task.AsValueTask();
    }
}
