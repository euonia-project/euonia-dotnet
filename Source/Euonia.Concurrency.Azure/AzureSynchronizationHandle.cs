using Nerosoft.Euonia.Concurrency.Azure.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// Implements <see cref="ISynchronizationHandle"/>
/// </summary>
public sealed class AzureSynchronizationHandle : ISynchronizationHandle
{
    private InternalHandle _internalHandle;
    private IDisposable _finalizerRegistration;

    internal AzureSynchronizationHandle(InternalHandle internalHandle)
    {
        _internalHandle = internalHandle;
        // Because this is a lease, managed finalization mostly won't be strictly necessary here. Where it comes in handy is:
        // (1) Ensuring blob deletion if we own the blob
        // (2) Helping release infinite-duration leases (rare case)
        // (3) In testing, avoiding having to wait 15+ seconds for lease expiration
        _finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, internalHandle);
    }

    /// <summary>
    /// Implements <see cref="ISynchronizationHandle.HandleCancellationToken"/>
    /// </summary>
    public CancellationToken HandleCancellationToken => (_internalHandle ?? throw this.ObjectDisposed()).HandleCancellationToken;

    /// <summary>
    /// The underlying Azure lease ID
    /// </summary>
    public string LeaseId => (_internalHandle ?? throw this.ObjectDisposed()).LeaseId;

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// Releases the lock asynchronously
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _finalizerRegistration, null)?.Dispose();
        return Interlocked.Exchange(ref _internalHandle, null)?.DisposeAsync() ?? default;
    }
}