using Azure.Storage.Blobs.Specialized;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

/// <summary>
/// Adds <see cref="TaskHelper"/> support to <see cref="BlobLeaseClient"/>
/// </summary>
internal sealed class BlobLeaseClientWrapper
{
    private readonly BlobLeaseClient _blobLeaseClient;

    public BlobLeaseClientWrapper(BlobLeaseClient blobLeaseClient)
    {
        _blobLeaseClient = blobLeaseClient;
    }

    public string LeaseId => _blobLeaseClient.LeaseId;

    public ValueTask AcquireAsync(TimeoutValue duration, CancellationToken cancellationToken)
    {
        if (TaskHelper.IsSynchronous)
        {
            _blobLeaseClient.Acquire(duration.TimeSpan, cancellationToken: cancellationToken);
            return default;
        }

        return new ValueTask(_blobLeaseClient.AcquireAsync(duration.TimeSpan, cancellationToken: cancellationToken));
    }

    public ValueTask RenewAsync(CancellationToken cancellationToken)
    {
        if (TaskHelper.IsSynchronous)
        {
            _blobLeaseClient.Renew(cancellationToken: cancellationToken);
            return default;
        }

        return new ValueTask(_blobLeaseClient.RenewAsync(cancellationToken: cancellationToken));
    }

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