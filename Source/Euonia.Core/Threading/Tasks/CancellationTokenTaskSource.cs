namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 持有取消令牌对应的任务以及令牌注册。当此实例被释放时，注册也会被释放。
/// </summary>
public sealed class CancellationTokenTaskSource<T> : IDisposable
{
    /// <summary>
    /// 取消令牌注册（如果存在）。如果不需要注册，则为 <c>null</c>。
    /// </summary>
    private readonly IDisposable _registration;

    /// <summary>
    /// 为指定的取消令牌创建任务，并在必要时注册到令牌。
    /// </summary>
    /// <param name="cancellationToken">要观察的取消令牌。</param>
    public CancellationTokenTaskSource(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Task = System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
            return;
        }
        var tcs = new TaskCompletionSource<T>();
        _registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
        Task = tcs.Task;
    }

    /// <summary>
    /// 获取源取消令牌对应的任务。
    /// </summary>
    public Task<T> Task { get; private set; }

    /// <summary>
    /// 释放取消令牌注册（如果存在）。请注意，这可能导致 <see cref="Task"/> 永远不会完成。
    /// </summary>
    public void Dispose()
    {
        _registration?.Dispose();
    }
}
