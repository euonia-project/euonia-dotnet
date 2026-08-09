using System.Diagnostics;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency;

/// <summary>
/// 监视租约（lease）的状态，并在租约丢失时发出信号。
/// </summary>
internal sealed class LeaseMonitor : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 用于释放监控循环的取消令牌源，以及用于通知租约丢失的取消令牌源。
    /// </summary>
    private readonly CancellationTokenSource _disposalSource = new(), _handleLostSource = new();

    /// <summary>
    /// 被监视的租约句柄。
    /// </summary>
    private readonly ILeaseHandle _leaseHandle;

    /// <summary>
    /// 后台监控循环任务。
    /// </summary>
    private readonly Task _monitoringTask;

    /// <summary>
    /// 当租约丢失时用于触发取消的后台任务。
    /// </summary>
    private Task _cancellationTask;

    /// <summary>
    /// 初始化 <see cref="LeaseMonitor"/> 类的新实例并启动监控循环。
    /// </summary>
    /// <param name="leaseHandle">要监视的租约句柄。</param>
    public LeaseMonitor(ILeaseHandle leaseHandle)
    {
        Invariant.Require(leaseHandle.LeaseDuration.CompareTo(leaseHandle.MonitoringCadence) >= 0);

        _leaseHandle = leaseHandle;
        _monitoringTask = CreateMonitoringLoopTask(new WeakReference<LeaseMonitor>(this), leaseHandle.MonitoringCadence, _disposalSource.Token);
    }

    /// <summary>
    /// 获取一个令牌，当租约丢失时该令牌将被取消。
    /// </summary>
    public CancellationToken HandleLostToken => _handleLostSource.Token;

    /// <summary>
    /// 同步释放监视器。
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// 异步释放监视器，取消监控循环并释放资源。
    /// </summary>
    /// <returns>表示异步释放操作的任务。</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_disposalSource.IsCancellationRequested) // 幂等操作
            {
                _disposalSource.Cancel();
            }

            await _monitoringTask.AwaitSyncOverAsync().ConfigureAwait(false);
        }
        finally
        {
            if (_cancellationTask != null)
            {
                _ = _cancellationTask.ContinueWith((_, state) => ((CancellationTokenSource)state).Dispose(), state: _handleLostSource, cancellationToken: HandleLostToken);
            }
            else
            {
                _handleLostSource.Dispose();
            }

            _disposalSource.Dispose();
        }
    }

    /// <summary>
    /// 创建后台监控循环任务，按指定的监视节奏定期检查租约状态。
    /// </summary>
    /// <param name="weakMonitor">监视器的弱引用，用于避免循环引用。</param>
    /// <param name="monitoringCadence">两次监视检查之间的间隔。</param>
    /// <param name="disposalToken">用于停止监控循环的取消令牌。</param>
    /// <returns>表示后台监控循环的任务。</returns>
    private static Task CreateMonitoringLoopTask(WeakReference<LeaseMonitor> weakMonitor, TimeoutValue monitoringCadence, CancellationToken disposalToken)
    {
        return Task.Run(MonitoringLoop, disposalToken);

        async Task MonitoringLoop()
        {
            var leaseLifetime = Stopwatch.StartNew();
            do
            {
                // 等待下一次监视检查
                await Task.Delay(monitoringCadence.InMilliseconds, disposalToken).TryAwait();
            }
            while (!disposalToken.IsCancellationRequested && await RunMonitoringLoopIterationAsync(weakMonitor, leaseLifetime).ConfigureAwait(false));
        }
    }

    /// <summary>
    /// 执行一轮监视检查，根据租约状态决定是否继续监控。
    /// </summary>
    /// <param name="weakMonitor">监视器的弱引用。</param>
    /// <param name="leaseLifetime">记录租约剩余生命周期的计时器。</param>
    /// <returns>如果应继续监控则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static async Task<bool> RunMonitoringLoopIterationAsync(WeakReference<LeaseMonitor> weakMonitor, Stopwatch leaseLifetime)
    {
        // 如果监视器已被垃圾回收，则直接退出
        if (!weakMonitor.TryGetTarget(out var monitor))
        {
            return false;
        }

        // 租约已过期
        if (monitor._leaseHandle.LeaseDuration.CompareTo(leaseLifetime.Elapsed) < 0)
        {
            OnHandleLost();
            return false;
        }

        var leaseState = await monitor.CheckLeaseAsync().ConfigureAwait(false);
        switch (leaseState)
        {
            case LeaseState.Lost:
                OnHandleLost();
                return false;

            case LeaseState.Renewed:
                leaseLifetime.Restart();
                return true;

            // 如果租约仍被持有但未续约，或状态未知（例如由于瞬时故障），
            // 则继续监控。此时尚不能断定租约已丢失，但因其未续约也无法重置生命周期。
            case LeaseState.Held:
            case LeaseState.Unknown:
                return true;

            default:
                throw new InvalidOperationException("should never get here");
        }

        // 将取消操作卸载到后台线程，以避免挂起或错误
        void OnHandleLost() => monitor._cancellationTask = Task.Run(() => monitor._handleLostSource.Cancel());
    }

    /// <summary>
    /// 检查当前租约的状态。
    /// </summary>
    /// <returns>当前租约的 <see cref="LeaseState"/> 状态。</returns>
    private async Task<LeaseState> CheckLeaseAsync()
    {
        var renewOrValidateTask = Helpers.SafeCreateTask(state => state.leaseHandle.RenewOrValidateLeaseAsync(state.Token), (leaseHandle: _leaseHandle, _disposalSource.Token));
        await renewOrValidateTask.TryAwait();
        return _disposalSource.IsCancellationRequested || renewOrValidateTask.Status != TaskStatus.RanToCompletion
            ? LeaseState.Unknown
            : renewOrValidateTask.Result;
    }

    /// <summary>
    /// 表示一个可被监视的租约句柄。
    /// </summary>
    public interface ILeaseHandle
    {
        /// <summary>
        /// 获取租约的持续时间。
        /// </summary>
        TimeoutValue LeaseDuration { get; }

        /// <summary>
        /// 获取监视节奏，即两次监视检查之间的间隔。
        /// </summary>
        TimeoutValue MonitoringCadence { get; }

        /// <summary>
        /// 续约或验证租约。
        /// </summary>
        /// <param name="cancellationToken">用于取消操作的令牌。</param>
        /// <returns>表示异步操作的任务，包含续约或验证后的租约状态。</returns>
        Task<LeaseState> RenewOrValidateLeaseAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 定义租约的状态。
    /// </summary>
    public enum LeaseState
    {
        /// <summary>
        /// 租约已知仍被持有，但尚未续约。
        /// </summary>
        Held,

        /// <summary>
        /// 租约已按 <see cref="ILeaseHandle.LeaseDuration"/> 续约。
        /// </summary>
        Renewed,

        /// <summary>
        /// 租约已知不再被持有。
        /// </summary>
        Lost,

        /// <summary>
        /// 租约是否仍被持有尚不确定。
        /// </summary>
        Unknown,
    }
}