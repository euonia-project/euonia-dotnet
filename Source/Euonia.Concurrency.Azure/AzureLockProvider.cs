using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Nerosoft.Euonia.Concurrency.Azure.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// 基于 Azure Blob 租约实现 <see cref="ILockProvider"/>。
/// </summary>
public sealed partial class AzureLockProvider : ILockProvider<AzureSynchronizationHandle>
{
    /// <summary>
    /// 元数据标记，用于指示某个 Blob 是为分布式锁创建的，因此在释放时应将其销毁。
    /// </summary>
    private const string CREATED_METADATA_KEY = "__EUONIA_LOCK__";

    /// <summary>
    /// 用于实现锁的 Blob 客户端包装器。
    /// </summary>
    internal readonly BlobClientWrapper BlobClient;

    /// <summary>
    /// Azure 同步配置选项。
    /// </summary>
    internal readonly AzureSynchronizationOptions Options;

    /// <summary>
    /// 构造一个将对提供的 <paramref name="blobClient"/> 进行租约的锁。
    /// </summary>
    /// <param name="blobClient">要租约的 Blob 客户端。</param>
    /// <param name="options">用于配置同步选项的可选委托。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="blobClient"/> 为 <c>null</c> 时抛出。</exception>
    public AzureLockProvider(BlobBaseClient blobClient, Action<AzureSynchronizationOptionsBuilder> options = null)
    {
        BlobClient = new BlobClientWrapper(blobClient ?? throw new ArgumentNullException(nameof(blobClient)));
        Options = AzureSynchronizationOptionsBuilder.GetOptions(options);
    }

    /// <summary>
    /// 在提供的 <paramref name="blobContainerClient"/> 内，基于 <paramref name="name"/> 构造一个将租约某个 Blob 的锁。
    /// </summary>
    /// <param name="blobContainerClient">包含目标 Blob 的容器客户端。</param>
    /// <param name="name">用于定位 Blob 的名称。</param>
    /// <param name="options">用于配置同步选项的可选委托。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="blobContainerClient"/> 或 <paramref name="name"/> 为 <c>null</c> 时抛出。</exception>
    public AzureLockProvider(BlobContainerClient blobContainerClient, string name, Action<AzureSynchronizationOptionsBuilder> options = null)
    {
        if (blobContainerClient == null)
        {
            throw new ArgumentNullException(nameof(blobContainerClient));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        BlobClient = new BlobClientWrapper(blobContainerClient.GetBlobClient(GetSafeName(name, blobContainerClient)));
        Options = AzureSynchronizationOptionsBuilder.GetOptions(options);
    }

    /// <inheritdoc />
    public string Name => BlobClient.Name;

    // 实现基于 https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
    /// <summary>
    /// 将名称转换为符合 Azure Blob 命名规则的安全名称。
    /// 当连接的是存储模拟器时允许的最大长度较短。
    /// </summary>
    /// <param name="name">原始名称。</param>
    /// <param name="blobContainerClient">用于检测存储模拟器的容器客户端。</param>
    /// <returns>符合 Azure Blob 命名规则的安全名称。</returns>
    private static string GetSafeName(string name, BlobContainerClient blobContainerClient)
    {
        var maxLength = IsStorageEmulator() ? 256 : 1024;

        return Helpers.ToSafeName(name, maxLength, s => ConvertToValidName(s));

        // 检查基于
        // https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#connect-to-the-emulator-account-using-the-well-known-account-name-and-key
        bool IsStorageEmulator() => blobContainerClient.Uri.IsAbsoluteUri
                                    && blobContainerClient.Uri.AbsoluteUri.StartsWith("http://127.0.0.1:10000/devstoreaccount1", StringComparison.Ordinal);

        static string ConvertToValidName(string name)
        {
            const int maxSlashes = 253; // 最多允许 254 个段，即 253 个斜杠

            if (name.Length == 0)
            {
                return "__EMPTY__";
            }

            StringBuilder builder = null;
            var slashCount = 0;
            for (var i = 0; i < name.Length; ++i)
            {
                var @char = name[i];

                // 限制路径段的数量，并注意避免使用尾随斜杠或点号

                if ((@char == '/' || @char == '\\')
                    && (++slashCount > maxSlashes || i == name.Length - 1))
                {
                    EnsureBuilder().Append("SLASH");
                }
                else if (@char == '.' && i == name.Length - 1)
                {
                    EnsureBuilder().Append("DOT");
                }
                else
                {
                    builder?.Append(@char);
                }

                StringBuilder EnsureBuilder() => builder ??= new StringBuilder().Append(name, startIndex: 0, count: i);
            }

            return builder?.ToString() ?? name;
        }
    }

    /// <summary>
    /// 尝试异步获取锁。
    /// 在 Blob 上获取租约；若租约已存在则返回 <c>null</c>，若 Blob 不存在则创建后重试。
    /// </summary>
    /// <param name="leaseClient">用于获取租约的 Blob 租约客户端。</param>
    /// <param name="isRetryAfterCreate">指示本次尝试是否发生在创建 Blob 之后。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务，包含获取到的锁句柄；失败时返回 <c>null</c>。</returns>
    private async ValueTask<AzureSynchronizationHandle> TryAcquireAsync(BlobLeaseClientWrapper leaseClient, bool isRetryAfterCreate, CancellationToken cancellationToken)
    {
        try
        {
            await leaseClient.AcquireAsync(Options.Duration, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException acquireException)
        {
            switch (acquireException.ErrorCode)
            {
                case AzureErrors.LeaseAlreadyPresent:
                // 如果刚创建后 Blob 又不存在，则返回 null 并稍后重试
                case AzureErrors.BlobNotFound when isRetryAfterCreate:
                    return null;
                // 创建 Blob
                case AzureErrors.BlobNotFound:
                {
                    var metadata = new Dictionary<string, string> { [CREATED_METADATA_KEY] = DateTime.UtcNow.ToString("o") }; // 日期值仅用于调试
                    try
                    {
                        await BlobClient.CreateIfNotExistsAsync(metadata, cancellationToken).ConfigureAwait(false);
                    }
                    catch (RequestFailedException createException)
                    {
                        // 处理竞争条件：尝试创建时其他人已先创建
                        return createException.ErrorCode == AzureErrors.LeaseIdMissing
                            ? default
                            : throw new AggregateException($"Blob {BlobClient.Name} does not exist and could not be created. See inner exceptions for details", acquireException, createException);
                    }

                    try
                    {
                        return await TryAcquireAsync(leaseClient, isRetryAfterCreate: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception retryException)
                    {
                        // 如果重试失败且 Blob 是由我们创建的，则尝试删除以进行清理
                        try
                        {
                            await BlobClient.DeleteIfExistsAsync().ConfigureAwait(false);
                        }
                        catch (Exception deletionException)
                        {
                            throw new AggregateException(retryException, deletionException);
                        }

                        throw;
                    }
                }
                default:
                    throw;
            }
        }

        var shouldDeleteBlob = isRetryAfterCreate
                               || (await BlobClient.GetMetadataAsync(leaseClient.LeaseId, cancellationToken).ConfigureAwait(false)).ContainsKey(CREATED_METADATA_KEY);

        var internalHandle = new InternalHandle(leaseClient, ownsBlob: shouldDeleteBlob, @lock: this);
        return new AzureSynchronizationHandle(internalHandle);
    }
}

public sealed partial class AzureLockProvider
{
	/// <inheritdoc />
	public AzureSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.Acquire(this, timeout, cancellationToken);
    }

	/// <inheritdoc />
	public ValueTask<AzureSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }

	/// <inheritdoc />
	public AzureSynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return Helpers.TryAcquire(this, timeout, cancellationToken);
    }

	/// <inheritdoc />
	public ValueTask<AzureSynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return this.As<ILockProvider<AzureSynchronizationHandle>>().TryAcquireAsync(timeout, cancellationToken);
    }

	/// <inheritdoc />
	public ValueTask<AzureSynchronizationHandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return BusyWaitHelper.WaitAsync(
            (@lock: this, leaseClient: BlobClient.GetBlobLeaseClient()),
            (state, token) => state.@lock.TryAcquireAsync(state.leaseClient, isRetryAfterCreate: false, cancellationToken: token),
            timeout,
            minSleepTime: Options.MinBusyWaitSleepTime,
            maxSleepTime: Options.MaxBusyWaitSleepTime,
            cancellationToken
        );
    }
}

public sealed partial class AzureLockProvider
{
    /// <inheritdoc />
    ISynchronizationHandle ILockProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    /// <inheritdoc />
    ISynchronizationHandle ILockProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask<ISynchronizationHandle> ILockProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    /// <inheritdoc />
    ValueTask<ISynchronizationHandle> ILockProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    /// <inheritdoc />
    ValueTask<ISynchronizationHandle> ILockProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }
}