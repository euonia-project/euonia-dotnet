namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// Implements <see cref="ILockFactory"/> for <see cref="FileLockProvider"/>
/// </summary>
public sealed class FileSynchronizationFactory : ILockFactory
{
    private readonly DirectoryInfo _lockFileDirectory;

    /// <summary>
    /// Constructs a provider that scopes lock files within the provided <paramref name="lockFileDirectory"/>.
    /// </summary>
    public FileSynchronizationFactory(DirectoryInfo lockFileDirectory)
    {
        _lockFileDirectory = lockFileDirectory ?? throw new ArgumentNullException(nameof(lockFileDirectory));
    }

    /// <summary>
    /// Constructs a <see cref="FileLockProvider"/> with the given <paramref name="name"/>.
    /// </summary>
    private FileLockProvider Create(string name) => new(_lockFileDirectory, name);

    ILockProvider ILockFactory.Create(string name) => Create(name);
}
