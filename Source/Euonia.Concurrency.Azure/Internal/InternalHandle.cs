

using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

internal sealed class InternalHandle : ISynchronizationHandle, LeaseMonitor.ILeaseHandle
{
    private readonly BlobLeaseClientWrapper _leaseClient;
    private readonly bool _ownsBlob;
    private readonly AzureLockProvider _lock;
    private readonly LeaseMonitor _leaseMonitor;

    public InternalHandle(BlobLeaseClientWrapper leaseClient, bool ownsBlob, AzureLockProvider @lock)
    {
        _leaseClient = leaseClient;
        _ownsBlob = ownsBlob;
        _lock = @lock;
        _leaseMonitor = new LeaseMonitor(this);
    }

    public CancellationToken HandleCancellationToken => _leaseMonitor.HandleLostToken;

    private bool RenewalEnabled => !_lock.Options.RenewalCadence.IsInfinite;

    public string LeaseId => _leaseClient.LeaseId;

    TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => _lock.Options.Duration;

    TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence => RenewalEnabled ? _lock.Options.RenewalCadence : _lock.Options.Duration;

    public void Dispose() => this.DisposeSyncViaAsync();

    public async ValueTask DisposeAsync()
    {
        // note that we're not trying to be idempotent here since we'll be wrapped
        // by AzureSynchronizationHandle which provides idempotence

        await _leaseMonitor.DisposeAsync().ConfigureAwait(false);

        // if we own the blob, release by just deleting it
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
            // if we're not renewing, then just touch the blob using the lease to see if someone else has renewed it
            : _lock.BlobClient.GetMetadataAsync(_leaseClient.LeaseId, cancellationToken).AsTask();

        await task.TryAwait();
        cancellationToken.ThrowIfCancellationRequested(); // if the cancellation caused failure, don't confuse that with losing the handle
        return task.Status == TaskStatus.RanToCompletion
            ? RenewalEnabled
                ? LeaseMonitor.LeaseState.Renewed
                : LeaseMonitor.LeaseState.Held
            : LeaseMonitor.LeaseState.Lost;
    }
}