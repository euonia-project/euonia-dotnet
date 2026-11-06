using Azure.Storage.Blobs.Specialized;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

/// <summary>
/// 为 <see cref="BlobLeaseClient"/> 添加 <see cref="TaskHelper"/> 支持。
/// </summary>
internal sealed class BlobLeaseClientWrapper
{
    private readonly BlobLeaseClient _blobLeaseClient;

    /// <summary>
    /// 初始化 <see cref="BlobLeaseClientWrapper"/> 的新实例。
    /// </summary>
    /// <param name="blobLeaseClient">要包装的 Blob 租约客户端。</param>
    public BlobLeaseClientWrapper(BlobLeaseClient blobLeaseClient)
    {
        _blobLeaseClient = blobLeaseClient;
    }

    /// <summary>
    /// 获取当前租约的 ID。
    /// </summary>
    /// <value>租约 ID。</value>
    public string LeaseId => _blobLeaseClient.LeaseId;

    /// <summary>
    /// 获取指定时长的租约。
    /// </summary>
    /// <param name="duration">租约时长。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public ValueTask AcquireAsync(TimeoutValue duration, CancellationToken cancellationToken)
    {
        if (TaskHelper.IsSynchronous)
        {
            _blobLeaseClient.Acquire(duration.TimeSpan, cancellationToken: cancellationToken);
            return default;
        }

        return new ValueTask(_blobLeaseClient.AcquireAsync(duration.TimeSpan, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// 续订当前租约。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public ValueTask RenewAsync(CancellationToken cancellationToken)
    {
        if (TaskHelper.IsSynchronous)
        {
            _blobLeaseClient.Renew(cancellationToken: cancellationToken);
            return default;
        }

        return new ValueTask(_blobLeaseClient.RenewAsync(cancellationToken: cancellationToken));
    }

    /// <summary>
    /// 释放当前租约。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    public ValueTask ReleaseAsync()
    {
        if (TaskHelper.IsSynchronous)
        {
            _blobLeaseClient.Release();
            return default;
        }

        return new ValueTask(_blobLeaseClient.ReleaseAsync());
    }
}