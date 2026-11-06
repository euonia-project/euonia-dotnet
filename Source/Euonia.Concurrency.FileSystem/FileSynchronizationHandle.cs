namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 基于文件流的同步句柄；释放时关闭文件流并删除锁文件。
/// </summary>
public sealed class FileSynchronizationHandle : ISynchronizationHandle
{
    private FileStream? _fileStream;

    /// <summary>
    /// 使用已保持打开的锁文件流初始化 <see cref="FileSynchronizationHandle"/>。
    /// </summary>
    /// <param name="fileStream">独占打开的锁文件流，释放句柄时会被关闭。</param>
    internal FileSynchronizationHandle(FileStream fileStream)
    {
        _fileStream = fileStream;
    }

    CancellationToken ISynchronizationHandle.HandleCancellationToken =>
        Volatile.Read(ref _fileStream) != null ? CancellationToken.None : throw this.ObjectDisposed();

    /// <summary>
    /// 释放文件流并删除锁文件。
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref _fileStream, null)?.Dispose();

    /// <summary>
    /// 异步释放文件流并删除锁文件。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
