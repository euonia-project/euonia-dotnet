namespace Nerosoft.Euonia.Threading.Interop;

/// <summary>
/// 为 <see cref="WaitHandle"/> 类型提供互操作工具方法。
/// </summary>
public static class WaitHandleAsyncFactory
{
    /// <summary>
    /// 使用 <see cref="Task"/> 包装一个 <see cref="WaitHandle"/>。当 <see cref="WaitHandle"/> 收到信号时，返回的 <see cref="Task"/> 完成。如果句柄已经收到信号，此方法会同步执行。
    /// </summary>
    /// <param name="handle">要观察的 <see cref="WaitHandle"/>。</param>
    public static Task FromWaitHandle(WaitHandle handle)
    {
        return FromWaitHandle(handle, Timeout.InfiniteTimeSpan, CancellationToken.None);
    }

    /// <summary>
    /// 使用 <see cref="Task{Boolean}"/> 包装一个 <see cref="WaitHandle"/>。如果 <see cref="WaitHandle"/> 收到信号，返回的任务将以 <c>true</c> 结果完成。如果观察超时，返回的任务将以 <c>false</c> 结果完成。如果句柄已经收到信号或超时时间为零，此方法会同步执行。
    /// </summary>
    /// <param name="handle">要观察的 <see cref="WaitHandle"/>。</param>
    /// <param name="timeout">在此时间之后不再观察 <see cref="WaitHandle"/> 的超时时间。</param>
    public static Task<bool> FromWaitHandle(WaitHandle handle, TimeSpan timeout)
    {
        return FromWaitHandle(handle, timeout, CancellationToken.None);
    }

    /// <summary>
    /// 使用 <see cref="Task{Boolean}"/> 包装一个 <see cref="WaitHandle"/>。如果 <see cref="WaitHandle"/> 收到信号，返回的任务将（成功）完成。如果观察被取消，返回的任务将被取消。如果句柄已经收到信号或取消令牌已经被取消，此方法会同步执行。
    /// </summary>
    /// <param name="handle">要观察的 <see cref="WaitHandle"/>。</param>
    /// <param name="token">用于取消观察 <see cref="WaitHandle"/> 的取消令牌。</param>
    public static Task FromWaitHandle(WaitHandle handle, CancellationToken token)
    {
        return FromWaitHandle(handle, Timeout.InfiniteTimeSpan, token);
    }

    /// <summary>
    /// 使用 <see cref="Task{Boolean}"/> 包装一个 <see cref="WaitHandle"/>。如果 <see cref="WaitHandle"/> 收到信号，返回的任务将以 <c>true</c> 结果完成。如果观察超时，返回的任务将以 <c>false</c> 结果完成。如果观察被取消，返回的任务将被取消。如果句柄已经收到信号、超时时间为零或取消令牌已经被取消，则此方法会同步执行。
    /// </summary>
    /// <param name="handle">要观察的 <see cref="WaitHandle"/>。</param>
    /// <param name="timeout">在此时间之后不再观察 <see cref="WaitHandle"/> 的超时时间。</param>
    /// <param name="token">用于取消观察 <see cref="WaitHandle"/> 的取消令牌。</param>
    public static Task<bool> FromWaitHandle(WaitHandle handle, TimeSpan timeout, CancellationToken token)
    {
        // 处理同步情况。
        // Handle synchronous cases.
        var alreadySignalled = handle.WaitOne(0);
        if (alreadySignalled)
            return TaskConstants.BooleanTrue;
        if (timeout == TimeSpan.Zero)
            return TaskConstants.BooleanFalse;
        if (token.IsCancellationRequested)
            return TaskConstants<bool>.Canceled;

        // 注册所有异步情况。
        // Register all asynchronous cases.
        return DoFromWaitHandle(handle, timeout, token);
    }

    private static async Task<bool> DoFromWaitHandle(WaitHandle handle, TimeSpan timeout, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (new ThreadPoolRegistration(handle, timeout, tcs))
        using (token.Register(state => ((TaskCompletionSource<bool>)state).TrySetCanceled(), tcs,
                   useSynchronizationContext: false))
        {
            return await tcs.Task.ConfigureAwait(false);
        }
    }

    private sealed class ThreadPoolRegistration : IDisposable
    {
        private readonly RegisteredWaitHandle _registeredWaitHandle;

        public ThreadPoolRegistration(WaitHandle handle, TimeSpan timeout, TaskCompletionSource<bool> tcs)
        {
            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(handle,
                (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut), tcs,
                timeout, executeOnlyOnce: true);
        }

        void IDisposable.Dispose() => _registeredWaitHandle.Unregister(null);
    }
}
