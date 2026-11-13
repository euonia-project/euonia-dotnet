using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

/// <summary>
/// 为 <see cref="BlobBaseClient"/> 添加 <see cref="TaskHelper"/> 支持。
/// </summary>
internal class BlobClientWrapper
{
    private readonly BlobBaseClient _blobClient;

    /// <summary>
    /// 初始化 <see cref="BlobClientWrapper"/> 的新实例。
    /// </summary>
    /// <param name="blobClient">要包装的 Blob 客户端，其具体类型决定创建与删除时使用的 API。</param>
    public BlobClientWrapper(BlobBaseClient blobClient)
    {
        _blobClient = blobClient;
    }

    /// <summary>
    /// 获取底层 Blob 的名称。
    /// </summary>
    /// <value>Blob 名称。</value>
    public string Name => _blobClient.Name;

    /// <summary>
    /// 创建用于该 Blob 的租约客户端包装。
    /// </summary>
    /// <returns>包装后的租约客户端。</returns>
    public BlobLeaseClientWrapper GetBlobLeaseClient() => new(_blobClient.GetBlobLeaseClient());

    /// <summary>
    /// 在指定租约下读取 Blob 的元数据。
    /// </summary>
    /// <param name="leaseId">租约 ID，作为请求条件随调用发送。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>Blob 的元数据键值集合。</returns>
    /// <remarks>同步执行路径下改用同步 API，避免阻塞异步续延造成死锁。</remarks>
    public async ValueTask<IDictionary<string, string>> GetMetadataAsync(string leaseId, CancellationToken cancellationToken)
    {
        var conditions = new BlobRequestConditions { LeaseId = leaseId };

        // 同步路径必须调用同步 API：同步阻塞等待异步调用会把续延投递回已被阻塞的
        // SynchronizationContext，在 WinForms/WPF 等环境下造成死锁。
        var properties = TaskHelper.IsSynchronous
            ? _blobClient.GetProperties(conditions, cancellationToken)
            : await _blobClient.GetPropertiesAsync(conditions, cancellationToken).ConfigureAwait(false);
        return properties.Value.Metadata;
    }

    /// <summary>
    /// 确保 Blob 存在：以空内容与指定元数据上传，具体行为取决于底层 Blob 类型。
    /// </summary>
    /// <param name="metadata">要写入的元数据。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <exception cref="InvalidOperationException">当底层客户端不是受支持的 Blob 类型时抛出。</exception>
    public ValueTask CreateIfNotExistsAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        switch (_blobClient)
        {
            case BlobClient blobClient:
                if (TaskHelper.IsSynchronous)
                {
                    blobClient.Upload(Stream.Null, metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }

                return new ValueTask(blobClient.UploadAsync(Stream.Null, metadata: metadata, cancellationToken: cancellationToken));
            case BlockBlobClient blockBlobClient:
                if (TaskHelper.IsSynchronous)
                {
                    blockBlobClient.Upload(Stream.Null, metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }

                return new ValueTask(blockBlobClient.UploadAsync(Stream.Null, metadata: metadata, cancellationToken: cancellationToken));
            case PageBlobClient pageBlobClient:
                if (TaskHelper.IsSynchronous)
                {
                    pageBlobClient.CreateIfNotExists(size: 0, metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }

                return new ValueTask(pageBlobClient.CreateIfNotExistsAsync(size: 0, metadata: metadata, cancellationToken: cancellationToken));
            case AppendBlobClient appendBlobClient:
                if (TaskHelper.IsSynchronous)
                {
                    appendBlobClient.CreateIfNotExists(metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }

                return new ValueTask(appendBlobClient.CreateIfNotExistsAsync(metadata: metadata, cancellationToken: cancellationToken));
            default:
                throw new InvalidOperationException(
                    _blobClient.GetType() == typeof(BlobBaseClient)
                        ? string.Format(Resources.IDS_LOCK_BLOB_BASE_CLIENT_TYPE_NOT_SUPPORTED, typeof(BlobBaseClient), typeof(BlobClient))
                        : string.Format(Resources.IDS_UNEXPECTED_BLOB_CLIENT_TYPE, _blobClient.GetType())
                );
        }
    }

    /// <summary>
    /// 删除 Blob；不存在时视为成功。
    /// </summary>
    /// <param name="leaseId">可选的租约 ID，指定后作为删除请求的条件。</param>
    /// <returns>表示异步操作的任务。</returns>
    public ValueTask DeleteIfExistsAsync(string? leaseId = null)
    {
        var conditions = leaseId != null ? new BlobRequestConditions { LeaseId = leaseId } : null;
        if (TaskHelper.IsSynchronous)
        {
            _blobClient.DeleteIfExists(conditions: conditions);
            return default;
        }

        {
        }
        return new ValueTask(_blobClient.DeleteIfExistsAsync(conditions: conditions));
    }
}