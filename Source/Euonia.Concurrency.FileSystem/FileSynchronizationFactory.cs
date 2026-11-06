namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 为 <see cref="FileLockProvider"/> 实现 <see cref="ILockFactory"/>
/// </summary>
public sealed class FileSynchronizationFactory : ILockFactory
{
    private readonly DirectoryInfo _lockFileDirectory;

    /// <summary>
    /// 构造一个将锁文件限定在所提供的 <paramref name="lockFileDirectory"/> 内的提供程序。
    /// </summary>
    public FileSynchronizationFactory(DirectoryInfo lockFileDirectory)
    {
        _lockFileDirectory = lockFileDirectory ?? throw new ArgumentNullException(nameof(lockFileDirectory));
    }

    /// <summary>
    /// 使用给定的 <paramref name="name"/> 构造一个 <see cref="FileLockProvider"/>。
    /// </summary>
    private FileLockProvider Create(string name) => new(_lockFileDirectory, name);

    ILockProvider ILockFactory.Create(string name) => Create(name);
}
