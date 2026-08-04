using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Nerosoft.Euonia.Concurrency.Azure.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// Implements a <see cref="ILockProvider"/> based on Azure blob leases
/// </summary>
public sealed partial class AzureLockProvider : ILockProvider<AzureSynchronizationHandle>
{
    /// <summary>
    /// Metadata marker used to indicate that a blob was created for distributed locking and therefore 
    /// should be destroyed upon release
    /// </summary>
    private const string CREATED_METADATA_KEY = "__EUONIA_LOCK__";

    internal readonly BlobClientWrapper BlobClient;
    internal readonly AzureSynchronizationOptions Options;

    /// <summary>
    /// Constructs a lock that will lease the provided <paramref name="blobClient"/>
    /// </summary>
    public AzureLockProvider(BlobBaseClient blobClient, Action<AzureSynchronizationOptionsBuilder> options = null)
    {
        BlobClient = new BlobClientWrapper(blobClient ?? throw new ArgumentNullException(nameof(blobClient)));
        Options = AzureSynchronizationOptionsBuilder.GetOptions(options);
    }

    /// <summary>
    /// Constructs a lock that will lease a blob based on <paramref name="name"/> within the provided <paramref name="blobContainerClient"/>.
    /// </summary>
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

    /// <summary>
    /// Implements <see cref="ILockProvider.Name"/>
    /// </summary>
    public string Name => BlobClient.Name;

    // implementation based on https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
    private static string GetSafeName(string name, BlobContainerClient blobContainerClient)
    {
        var maxLength = IsStorageEmulator() ? 256 : 1024;

        return Helpers.ToSafeName(name, maxLength, s => ConvertToValidName(s));

        // check based on 
        // https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#connect-to-the-emulator-account-using-the-well-known-account-name-and-key
        bool IsStorageEmulator() => blobContainerClient.Uri.IsAbsoluteUri
                                    && blobContainerClient.Uri.AbsoluteUri.StartsWith("http://127.0.0.1:10000/devstoreaccount1", StringComparison.Ordinal);

        static string ConvertToValidName(string name)
        {
            const int maxSlashes = 253; // allowed to have up to 254 segments, which means 253 slashes

            if (name.Length == 0)
            {
                return "__EMPTY__";
            }

            StringBuilder builder = null;
            var slashCount = 0;
            for (var i = 0; i < name.Length; ++i)
            {
                var @char = name[i];

                // enforce cap on # path segments and note that trailing slash or DOT are
                // discouraged

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
                // if we just created and it already doesn't exist again, just return null and retry later
                case AzureErrors.BlobNotFound when isRetryAfterCreate:
                    return null;
                // create the blob
                case AzureErrors.BlobNotFound:
                {
                    var metadata = new Dictionary<string, string> { [CREATED_METADATA_KEY] = DateTime.UtcNow.ToString("o") }; // date value is just for debugging
                    try
                    {
                        await BlobClient.CreateIfNotExistsAsync(metadata, cancellationToken).ConfigureAwait(false);
                    }
                    catch (RequestFailedException createException)
                    {
                        // handle the race condition where we try to create and someone else creates it first
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
                        // if the retry fails and we created, attempt deletion to clean things up
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
    ISynchronizationHandle ILockProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    ISynchronizationHandle ILockProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    ValueTask<ISynchronizationHandle> ILockProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ILockProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ILockProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }
}