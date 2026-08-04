using Azure.Storage.Blobs;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// Implements <see cref="ILockFactory"/> for <see cref="AzureLockProvider"/>
/// </summary>
public sealed class AzureSynchronizationFactory : ILockFactory
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly Action<AzureSynchronizationOptionsBuilder> _options;

    /// <summary>
    /// Constructs a provider that scopes blobs within the provided <paramref name="blobContainerClient"/> and uses the provided <paramref name="options"/>.
    /// </summary>
    public AzureSynchronizationFactory(BlobContainerClient blobContainerClient, Action<AzureSynchronizationOptionsBuilder> options = null)
    {
        _blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        _options = options;
    }

    /// <summary>
    /// Constructs an <see cref="AzureLockProvider"/> with the given <paramref name="name"/>.
    /// </summary>
    private AzureLockProvider Create(string name) => new(_blobContainerClient, name, _options);

    ILockProvider ILockFactory.Create(string name) => Create(name);
}