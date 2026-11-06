using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 基于持有锁文件独占句柄的分布式锁。释放锁时该文件将被删除。
/// </summary>
public sealed partial class FileLockProvider : ILockProvider<FileSynchronizationHandle>
{
    /// <summary>
    /// 由于 <see cref="UnauthorizedAccessException"/> 既可能是瞬时抛出，也可能是权限问题导致，因此在认定问题为非瞬时之前，我们最多重试此次数。
    /// 根据经验，无论是在本地还是在 AppVeyor 上，该值都是可靠的（如果确实存在问题，多试几次风险也很小，因为最终会明确失败）。
    /// </summary>
    private const int MAX_UNAUTHORIZED_ACCESS_EXCEPTION_RETRIES = 400;

    // 这些值目前不可配置，因为将来我们可能想改变 FileLockProvider 的实现，
    // 利用原生方法来实现真正的阻塞。这里的取值反映了我们的预期：文件锁
    // 会被用在竞争很少的场景中。
    private static readonly TimeoutValue _minBusyWaitSleepTime = TimeSpan.FromMilliseconds(50), _maxBusyWaitSleepTime = TimeSpan.FromSeconds(1);

    private string? _cachedDirectory;

    /// <summary>
    /// 构造一个锁，使用提供的 <paramref name="lockFile"/> 作为确切的文件名。
    /// 
    /// 获取锁时，如果文件所在目录尚不存在，将自动创建。文件同样会在不存在时
    /// 被创建，并在释放锁时被删除。
    /// </summary>
    public FileLockProvider(FileInfo lockFile)
    {
        Name = (lockFile ?? throw new ArgumentNullException(nameof(lockFile))).FullName;
        if (lockFile.Name.Length == 0)
        {
            throw new FormatException(Resources.IDS_LOCK_FILE_EMPTY_NAME);
        }
    }

    /// <summary>
    /// 构造一个锁，将锁文件放置在 <paramref name="lockFileDirectory"/> 中。文件名
    /// 将基于 <paramref name="name"/> 生成，但会进行适当的转义/哈希，以确保产生有效的文件名。
    /// 
    /// 获取锁时，如果文件所在目录尚不存在，将自动创建。文件同样会在不存在时
    /// 被创建，并在释放锁时被删除。
    /// </summary>
    public FileLockProvider(DirectoryInfo lockFileDirectory, string name)
    {
        Name = FileNameValidationHelper.GetLockFileName(lockFileDirectory, name);
    }

    /// <inheritdoc />
    public string Name { get; }

    private string Directory => _cachedDirectory ??= Path.GetDirectoryName(Name)!;

    private FileSynchronizationHandle? TryAcquire(CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDirectoryExists();

            FileStream lockFileStream;
            try
            {
                // 关键参数：
                // OpenOrCreate 以便文件存在与否都能正常工作
                // None 以获取独占锁
                // DeleteOnClose 以便自行清理
                lockFileStream = new FileStream(Name, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, bufferSize: 1, FileOptions.DeleteOnClose);
            }
            catch (DirectoryNotFoundException)
            {
                // 这几乎不可能发生，因为我们刚刚创建了目录，但在竞态条件下仍有可能。重试即可
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                // 这在少数几种情况下可能发生：

                // 该路径已经是目录，因此我们永远无法以文件方式打开它的句柄
                if (System.IO.Directory.Exists(Name))
                {
                    throw new InvalidOperationException(string.Format(Resources.IDS_LOCK_FILE_IS_DIRECTORY, Name));
                }

                // 文件存在且为只读
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(Name);
                }
                catch
                {
                    attributes = FileAttributes.Normal;
                } // 例如：可能因 FileNotFoundException 而失败

                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    // 我们本可以在检测到文件只读时放弃 DeleteOnClose 来支持这种情况，
                    // 但由于没有需求或使用场景，目前就直接抛出异常
                    throw new NotSupportedException(string.Format(Resources.IDS_LOCK_FILE_READ_ONLY, Name));
                }

                // 令人沮丧的是，由于并发的创建/删除操作，该错误可能是瞬时抛出的。先假定
                // 它是瞬时的，直接重试
                if (++retryCount <= MAX_UNAUTHORIZED_ACCESS_EXCEPTION_RETRIES)
                {
                    continue;
                }

                // 如果执行到这里，说明重试已耗尽：假定这是确实存在的权限问题
                throw;
            }
            // 这不应发生，因为我们会进行校验。但如果真的发生了（例如由于某种系统配置变更？），就抛出异常，
            // 以免落入 IOException 分支（PathTooLongException 属于 IOException）
            catch (PathTooLongException)
            {
                throw;
            }
            catch (IOException)
            {
                // 希望走到这里时，唯一的失败原因就是文件已被锁定
                return null;
            }

            return new FileSynchronizationHandle(lockFileStream);
        }
    }

    private void EnsureDirectoryExists()
    {
        var retryCount = 0;

        while (true)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                return;
            }
            // 这可能表示并发创建/删除期间的瞬时失败，也可能是权限问题。
            // 如果遇到，先假定为瞬时问题，除非它持续存在。
            catch (Exception ex) when ((ex is UnauthorizedAccessException || ex is IOException) && ++retryCount <= MAX_UNAUTHORIZED_ACCESS_EXCEPTION_RETRIES)
            {
                // 并发创建/删除目录时可能瞬时抛出 IOException（上游同样按瞬态错误重试）。
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(Resources.IDS_LOCK_FILE_DIRECTORY_CREATE_FAILED, Directory), ex);
            }
        }
    }
}

/// <summary>
/// 提供 <see cref="FileLockProvider"/> 的锁获取方法的分部实现。
/// </summary>
public sealed partial class FileLockProvider
{
    /// <inheritdoc />
    public FileSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return Helpers.TryAcquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public FileSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.Acquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<FileSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return TryAcquireAsync(new TimeoutValue(timeout), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<FileSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<FileSynchronizationHandle?> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default)
    {
        return BusyWaitHelper.WaitAsync(
            state: this,
            tryGetValue: (@this, token) => @this.TryAcquire(token).AsValueTask(),
            timeout: timeout,
            minSleepTime: _minBusyWaitSleepTime,
            maxSleepTime: _maxBusyWaitSleepTime,
            cancellationToken
        );
    }
}

/// <summary>
/// 显式实现非泛型 <see cref="ILockProvider"/> 接口成员。
/// </summary>
public sealed partial class FileLockProvider
{
    ISynchronizationHandle? ILockProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    ISynchronizationHandle ILockProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    ValueTask<ISynchronizationHandle?> ILockProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).ConvertNullable(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ILockProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle?> ILockProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).ConvertNullable(TaskConversion<ISynchronizationHandle>.ValueTask);
    }
}