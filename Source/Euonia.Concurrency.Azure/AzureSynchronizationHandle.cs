using Nerosoft.Euonia.Concurrency.Azure.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// 实现 <see cref="ISynchronizationHandle"/>。
/// </summary>
public sealed class AzureSynchronizationHandle : ISynchronizationHandle
{
    /// <summary>
    /// 内部租约句柄，持有实际的租约与 Blob 所有权信息。
    /// </summary>
    private InternalHandle _internalHandle;

    /// <summary>
    /// 终结器队列注册，用于在句柄未被显式释放时执行清理。
    /// </summary>
    private IDisposable _finalizerRegistration;

    /// <summary>
    /// 初始化 <see cref="AzureSynchronizationHandle"/> 类的新实例。
    /// </summary>
    /// <param name="internalHandle">内部租约句柄。</param>
    internal AzureSynchronizationHandle(InternalHandle internalHandle)
    {
        _internalHandle = internalHandle;
        // 由于这是租约，托管终结在此处大多不是严格必需的。其价值在于：
        // (1) 若我们拥有 Blob，确保 Blob 被删除
        // (2) 帮助释放无限时长的租约（少见情况）
        // (3) 在测试中，避免等待 15 秒以上的租约过期时间
        _finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, internalHandle);
    }

    /// <inheritdoc />
    public CancellationToken HandleCancellationToken => (_internalHandle ?? throw this.ObjectDisposed()).HandleCancellationToken;

    /// <summary>
    /// 底层的 Azure 租约 ID。
    /// </summary>
    public string LeaseId => (_internalHandle ?? throw this.ObjectDisposed()).LeaseId;

    /// <summary>
    /// 释放锁。
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// 异步释放锁。
    /// </summary>
    /// <returns>表示异步释放操作的任务。</returns>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _finalizerRegistration, null)?.Dispose();
        return Interlocked.Exchange(ref _internalHandle, null)?.DisposeAsync() ?? default;
    }
}