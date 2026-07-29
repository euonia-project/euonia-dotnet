namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 需要以线程安全方式实现一次性语义的可释放对象基类。此实例的所有释放操作都会阻塞直到释放完成。
/// </summary>
/// <typeparam name="T">派生可释放对象的"上下文"类型。由于上下文不应被修改，强烈建议将其设为不可变类型。</typeparam>
/// <remarks>
/// <para>如果多次调用 <see cref="DisposeAsync()"/>，只有第一次调用会执行释放代码。其他对 <see cref="DisposeAsync()"/> 的调用将等待释放完成。</para>
/// </remarks>
public abstract class AsyncSingleDisposable<T> : IAsyncDisposable
{
    /// <summary>
    /// 上下文。永远不会为 <c>null</c>。如果此实例已经释放（或正在释放），则为空。
    /// </summary>
    private readonly AsyncBoundActionField<T> _context;

    private readonly TaskCompletionSource<object> _tcs = new(TaskCreationOptions.DenyChildAttach);

    /// <summary>
    /// 为指定的上下文创建可释放对象。
    /// </summary>
    /// <param name="context">传递给 <see cref="DisposeAsync(T)"/> 的上下文。</param>
    protected AsyncSingleDisposable(T context)
    {
        _context = new AsyncBoundActionField<T>(DisposeAsync, context);
    }

    /// <summary>
    /// 获取此实例是否正在释放或已经释放。
    /// </summary>
    public bool IsDisposeStarted => _context.IsEmpty;

    /// <summary>
    /// 获取此实例是否已经释放（释放完成）。
    /// </summary>
    public bool IsDisposed => _tcs.Task.IsCompleted;

    /// <summary>
    /// 获取此实例是否正在释放中但尚未完成。
    /// </summary>
    public bool IsDisposing => IsDisposeStarted && !IsDisposed;

    /// <summary>
    /// 实际的释放方法，仅由 <see cref="DisposeAsync()"/> 调用一次。
    /// </summary>
    /// <param name="context">释放操作的上下文。</param>
    protected abstract ValueTask DisposeAsync(T context);

    /// <summary>
    /// 释放此实例。
    /// </summary>
    /// <remarks>
    /// <para>如果多次调用 <see cref="DisposeAsync()"/>，只有第一次调用会执行释放代码。其他对 <see cref="DisposeAsync()"/> 的调用将等待释放完成。</para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        var context = _context.TryGetAndUnset();
        if (context == null)
        {
            await _tcs.Task.ConfigureAwait(false);
            return;
        }
        try
        {
            await context.InvokeAsync();
        }
        finally
        {
            // TODO: 文档化异常仅由一个释放者观察，而非所有释放者。
            _tcs.TrySetResult(null);
        }
    }

    /// <summary>
    /// 尝试更新存储的上下文。如果此实例已经释放（或正在释放），则返回 <c>false</c>。
    /// </summary>
    /// <param name="contextUpdater">用于更新现有上下文的函数。如果多个线程同时尝试更新上下文，此函数可能会被调用多次。</param>
    protected bool TryUpdateContext(Func<T, T> contextUpdater) => _context.TryUpdateContext(contextUpdater);
}