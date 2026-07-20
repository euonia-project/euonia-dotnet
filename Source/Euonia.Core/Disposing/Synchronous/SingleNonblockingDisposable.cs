namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 需要以线程安全方式实现一次性语义的可释放对象基类。
/// </summary>
/// <typeparam name="T">派生可释放对象的"上下文"类型。由于上下文不应被修改，强烈建议将其设为不可变类型。</typeparam>
/// <remarks>
/// <para>如果多次调用 <see cref="Dispose()"/>，只有第一次调用会执行释放代码。其他对 <see cref="Dispose()"/> 的调用不会等待释放完成。</para>
/// </remarks>
public abstract class SingleNonblockingDisposable<T> : IDisposable
{
    /// <summary>
    /// 上下文。永远不会为 <c>null</c>。如果此实例已经释放（或正在释放），则为空。
    /// </summary>
    private readonly BoundActionField<T> _context;

    /// <summary>
    /// 为指定的上下文创建可释放对象。
    /// </summary>
    /// <param name="context">传递给 <see cref="Dispose(T)"/> 的上下文。</param>
    protected SingleNonblockingDisposable(T context)
    {
        _context = new BoundActionField<T>(Dispose, context);
    }

    /// <summary>
    /// 获取此实例是否已经释放（或正在释放）。
    /// </summary>
    public bool IsDisposed => _context.IsEmpty;

    /// <summary>
    /// 实际的释放方法，仅由 <see cref="Dispose()"/> 调用一次。
    /// </summary>
    /// <param name="context">释放操作的上下文。</param>
    protected abstract void Dispose(T context);

    /// <summary>
    /// 释放此实例。
    /// </summary>
    /// <remarks>
    /// <para>如果多次调用 <see cref="Dispose()"/>，只有第一次调用会执行释放代码。其他对 <see cref="Dispose()"/> 的调用不会等待释放完成。</para>
    /// </remarks>
    public void Dispose() => _context.TryGetAndUnset()?.Invoke();

    /// <summary>
    /// 尝试更新存储的上下文。如果此实例已经释放（或正在释放），则返回 <c>false</c>。
    /// </summary>
    /// <param name="contextUpdater">用于更新现有上下文的函数。如果多个线程同时尝试更新上下文，此函数可能会被调用多次。</param>
    protected bool TryUpdateContext(Func<T, T> contextUpdater) => _context.TryUpdateContext(contextUpdater);
}