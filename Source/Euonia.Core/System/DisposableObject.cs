namespace System;

/// <summary>
/// 表示可释放对象的基类。
/// </summary>
/// <remarks>
/// 此类提供 .NET 中可释放模式的基本实现。
/// </remarks>
public abstract class DisposableObject : IDisposable
{
    private readonly WeakEventManager _events = new();

    /// <summary>
    /// 当当前对象已被释放时发生。
    /// </summary>
    public event EventHandler<DisposedEventArgs> Disposed
    {
        add => _events.AddEventHandler(value);
        remove => _events.RemoveEventHandler(value);
    }

    /// <summary>
    /// 终结 <see cref="DisposableObject"/> 类的实例。
    /// </summary>
    ~DisposableObject()
    {
        Dispose(false);
        InvokeDisposedEvent(this, new DisposedEventArgs());
    }

    /// <summary>
    /// 执行与释放、释放或重置非托管资源相关的应用程序定义任务。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        InvokeDisposedEvent(this, new DisposedEventArgs());
    }

    /// <summary>
    /// 释放非托管资源以及可选的托管资源。
    /// </summary>
    /// <param name="disposing"><c>true</c> 表示释放托管和非托管资源；<c>false</c> 表示仅释放非托管资源。</param>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// 触发对象已释放事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="args">事件参数。</param>
    protected virtual void InvokeDisposedEvent(object sender, DisposedEventArgs args)
    {
        _events.HandleEvent(sender, args, nameof(Disposed));
    }
}