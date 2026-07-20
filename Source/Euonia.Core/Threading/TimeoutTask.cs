namespace Nerosoft.Euonia.Threading.Redis;

/// <summary>
/// 充当 <see cref="Task.Delay(TimeSpan, CancellationToken)"/> 的包装器，当 <see cref="TimeoutTask"/> 被释放时会自动清理。
/// </summary>
public readonly struct TimeoutTask : IDisposable
{
    private readonly CancellationTokenSource _cleanupTokenSource;
    private readonly CancellationTokenSource _linkedTokenSource;

    /// <summary>
    /// 使用指定的超时值初始化 <see cref="TimeoutTask"/> 的新实例。
    /// </summary>
    /// <param name="timeout">超时值。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public TimeoutTask(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        _cleanupTokenSource = new CancellationTokenSource();
        _linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cleanupTokenSource.Token)
            : null;
        Task = Task.Delay(timeout.TimeSpan, _linkedTokenSource?.Token ?? _cleanupTokenSource.Token);
    }

    /// <summary>
    /// 获取底层的 <see cref="Task"/>。
    /// </summary>
    public Task Task { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            _cleanupTokenSource.Cancel();
        }
        finally
        {
            _linkedTokenSource?.Dispose();
            _cleanupTokenSource.Dispose();
        }
    }
}