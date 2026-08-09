using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

/// <summary>
/// Adds <see cref="TaskHelper"/> support to <see cref="BlobBaseClient"/>
/// </summary>
internal class BlobClientWrapper
{
    private readonly BlobBaseClient _blobClient;

    public BlobClientWrapper(BlobBaseClient blobClient)
    {
        _blobClient = blobClient;
    }

    public string Name => _blobClient.Name;

    public BlobLeaseClientWrapper GetBlobLeaseClient() => new(_blobClient.GetBlobLeaseClient());

    public async ValueTask<IDictionary<string, string>> GetMetadataAsync(string leaseId, CancellationToken cancellationToken)
    {
        var conditions = new BlobRequestConditions { LeaseId = leaseId };
        var properties = TaskHelper.IsSynchronous
            ? await _blobClient.GetPropertiesAsync(conditions, cancellationToken)
            : await _blobClient.GetPropertiesAsync(conditions, cancellationToken).ConfigureAwait(false);
        return properties.Value.Metadata;
    }

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
                        ? $"Unable to create a lock blob given client type {typeof(BlobBaseClient)}. Either ensure that the blob exists or use a non-base client type such as {typeof(BlobClient)}"
                          + " which specifies the type of blob to create"
                        : $"Unexpected blob client type {_blobClient.GetType()}"
                );
        }
    }

    public ValueTask DeleteIfExistsAsync(string leaseId = null)
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