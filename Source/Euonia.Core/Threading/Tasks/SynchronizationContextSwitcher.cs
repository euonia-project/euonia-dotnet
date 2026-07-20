using Nerosoft.Euonia.Disposing;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 用于临时切换 <see cref="SynchronizationContext"/> 实现的工具类。
/// </summary>
public sealed class SynchronizationContextSwitcher : SingleDisposable<object>
{
    /// <summary>
    /// 之前的 <see cref="SynchronizationContext"/>。
    /// </summary>
    private readonly SynchronizationContext _oldContext;

    /// <summary>
    /// 初始化 <see cref="SynchronizationContextSwitcher"/> 类的新实例，并安装新的 <see cref="SynchronizationContext"/>。
    /// </summary>
    /// <param name="newContext">新的 <see cref="SynchronizationContext"/>。可以为 <c>null</c> 以移除现有的 <see cref="SynchronizationContext"/>。</param>
    private SynchronizationContextSwitcher(SynchronizationContext newContext)
        : base(new object())
    {
        _oldContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(newContext);
    }

    /// <summary>
    /// 恢复旧的 <see cref="SynchronizationContext"/>。
    /// </summary>
    protected override void Dispose(object context)
    {
        SynchronizationContext.SetSynchronizationContext(_oldContext);
    }

    /// <summary>
    /// 在没有当前 <see cref="SynchronizationContext"/> 的情况下执行同步委托。当前上下文在此函数返回时恢复。
    /// </summary>
    /// <param name="action">要执行的委托。</param>
    public static void NoContext(Action action)
    {
        using (new SynchronizationContextSwitcher(null))
            action();
    }

    /// <summary>
    /// 在没有当前 <see cref="SynchronizationContext"/> 的情况下执行同步或异步委托。当前上下文在此函数同步返回时恢复。
    /// </summary>
    /// <param name="action">要执行的委托。</param>
    public static T NoContext<T>(Func<T> action)
    {
        using (new SynchronizationContextSwitcher(null))
            return action();
    }

    /// <summary>
    /// 使用指定的 <see cref="SynchronizationContext"/> 作为"当前"上下文执行同步委托。之前的当前上下文在此函数返回时恢复。
    /// </summary>
    /// <param name="context">要视为"当前"的上下文。可以为 <c>null</c> 以指示线程池上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static void ApplyContext(SynchronizationContext context, Action action)
    {
        using (new SynchronizationContextSwitcher(context))
            action();
    }

    /// <summary>
    /// 使用指定的 <see cref="SynchronizationContext"/> 作为"当前"上下文执行同步或异步委托。之前的当前上下文在此函数同步返回时恢复。
    /// </summary>
    /// <param name="context">要视为"当前"的上下文。可以为 <c>null</c> 以指示线程池上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static T ApplyContext<T>(SynchronizationContext context, Func<T> action)
    {
        using (new SynchronizationContextSwitcher(context))
            return action();
    }
}
