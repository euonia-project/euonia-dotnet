

using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

/// <summary>
/// Azure Blob 锁的内部同步句柄：持有租约客户端，按配置续租，并在释放时删除 Blob 或归还租约。
/// </summary>
internal sealed class InternalHandle : ISynchronizationHandle, LeaseMonitor.ILeaseHandle
{
    private readonly BlobLeaseClientWrapper _leaseClient;
    private readonly bool _ownsBlob;
    private readonly AzureLockProvider _lock;
    private readonly LeaseMonitor _leaseMonitor;

    /// <summary>
    /// 初始化 <see cref="InternalHandle"/> 的新实例，并启动租约监视。
    /// </summary>
    /// <param name="leaseClient">已获取租约的客户端包装。</param>
    /// <param name="ownsBlob">为 <see langword="true"/> 时表示 Blob 由本句柄创建，释放时应删除而非归还租约。</param>
    /// <param name="lock">所属的 Azure 锁提供程序，提供租约时长与续租节奏等选项。</param>
    public InternalHandle(BlobLeaseClientWrapper leaseClient, bool ownsBlob, AzureLockProvider @lock)
    {
        _leaseClient = leaseClient;
        _ownsBlob = ownsBlob;
        _lock = @lock;
        _leaseMonitor = new LeaseMonitor(this);
    }

    /// <summary>
    /// 获取在租约丢失时触发的取消令牌。
    /// </summary>
    /// <value>租约监视器提供的句柄丢失令牌。</value>
    public CancellationToken HandleCancellationToken => _leaseMonitor.HandleLostToken;

    private bool RenewalEnabled => !_lock.Options.RenewalCadence.IsInfinite;

    /// <summary>
    /// 获取当前租约的 ID。
    /// </summary>
    /// <value>租约 ID。</value>
    public string LeaseId => _leaseClient.LeaseId;

    TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => _lock.Options.Duration;

    TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence => RenewalEnabled ? _lock.Options.RenewalCadence : _lock.Options.Duration;

    /// <summary>
    /// 同步释放句柄：停止租约监视，并删除自有 Blob 或归还租约。
    /// </summary>
    /// <remarks>幂等性由外层的 <c>AzureSynchronizationHandle</c> 包装保证。</remarks>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// 异步释放句柄：停止租约监视，并删除自有 Blob 或归还租约。
    /// </summary>
    /// <returns>表示异步释放操作的任务。</returns>
    /// <remarks>幂等性由外层的 <c>AzureSynchronizationHandle</c> 包装保证。</remarks>
    public async ValueTask DisposeAsync()
    {
        // 注意：这里不尝试实现幂等，因为外部会由 AzureSynchronizationHandle 包装，
        // 由它提供幂等性

        await _leaseMonitor.DisposeAsync().ConfigureAwait(false);

        // 如果该 Blob 归我们所有，则直接删除以释放
        if (_ownsBlob)
        {
            await _lock.BlobClient.DeleteIfExistsAsync(leaseId: _leaseClient.LeaseId).ConfigureAwait(false);
        }
        else
        {
            await _leaseClient.ReleaseAsync().ConfigureAwait(false);
        }
    }

    async Task<LeaseMonitor.LeaseState> LeaseMonitor.ILeaseHandle.RenewOrValidateLeaseAsync(CancellationToken cancellationToken)
    {
        var task = RenewalEnabled
            ? _leaseClient.RenewAsync(cancellationToken).AsTask()
            // 如果未续租，则仅使用租约访问该 Blob，以检查是否已被其他人续租
            : _lock.BlobClient.GetMetadataAsync(_leaseClient.LeaseId, cancellationToken).AsTask();

        await task.TryAwait();
        cancellationToken.ThrowIfCancellationRequested(); // 如果取消导致了失败，不要将其与丢失句柄混淆
        return task.Status == TaskStatus.RanToCompletion
            ? RenewalEnabled
                ? LeaseMonitor.LeaseState.Renewed
                : LeaseMonitor.LeaseState.Held
            : LeaseMonitor.LeaseState.Lost;
    }
}