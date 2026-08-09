namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// Implements <see cref="ISynchronizationHandle"/>
/// </summary>
public sealed class FileSynchronizationHandle : ISynchronizationHandle
{
    private FileStream _fileStream;

    internal FileSynchronizationHandle(FileStream fileStream)
    {
        _fileStream = fileStream;
    }

    CancellationToken ISynchronizationHandle.HandleCancellationToken =>
        Volatile.Read(ref _fileStream) != null ? CancellationToken.None : throw this.ObjectDisposed();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref _fileStream, null)?.Dispose();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
