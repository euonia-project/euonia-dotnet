using Azure.Storage.Blobs;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// 为 <see cref="AzureLockProvider"/> 实现 <see cref="ILockFactory"/>。
/// </summary>
public sealed class AzureSynchronizationFactory : ILockFactory
{
    /// <summary>
    /// 用于限定 Blob 作用域的 Blob 容器客户端。
    /// </summary>
    private readonly BlobContainerClient _blobContainerClient;

    /// <summary>
    /// 用于配置同步选项的可选委托。
    /// </summary>
    private readonly Action<AzureSynchronizationOptionsBuilder> _options;

    /// <summary>
    /// 构造一个工厂，其将 Blob 限定在提供的 <paramref name="blobContainerClient"/> 内，并使用提供的 <paramref name="options"/> 配置。
    /// </summary>
    /// <param name="blobContainerClient">用于限定 Blob 作用域的 Blob 容器客户端。</param>
    /// <param name="options">用于配置同步选项的可选委托。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="blobContainerClient"/> 为 <c>null</c> 时抛出。</exception>
    public AzureSynchronizationFactory(BlobContainerClient blobContainerClient, Action<AzureSynchronizationOptionsBuilder> options = null)
    {
        _blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        _options = options;
    }

    /// <summary>
    /// 使用给定的 <paramref name="name"/> 构造一个 <see cref="AzureLockProvider"/>。
    /// </summary>
    /// <param name="name">用于定位 Blob 的锁名称。</param>
    /// <returns>构造的 <see cref="AzureLockProvider"/> 实例。</returns>
    private AzureLockProvider Create(string name) => new(_blobContainerClient, name, _options);

    /// <inheritdoc />
    ILockProvider ILockFactory.Create(string name) => Create(name);
}