namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 提供等待某个值可用的方法。
/// </summary>
public static class BusyWaitHelper
{
    /// <summary>
    /// 通过重复调用 <paramref name="tryGetValue"/> 异步等待某个值变为可用，直到返回非 null 结果为止。
    /// </summary>
    /// <typeparam name="TState">传递给获取委托的状态类型。</typeparam>
    /// <typeparam name="TResult">要等待的结果类型。</typeparam>
    /// <param name="state">传递给 <paramref name="tryGetValue"/> 的状态对象。</param>
    /// <param name="tryGetValue">尝试获取值的委托，返回非 null 结果表示值已可用。</param>
    /// <param name="timeout">放弃等待之前的总超时时间。</param>
    /// <param name="minSleepTime">两次尝试之间的最小休眠时间。</param>
    /// <param name="maxSleepTime">两次尝试之间的最大休眠时间。</param>
    /// <param name="cancellationToken">用于取消等待操作的令牌。</param>
    /// <returns>获取到的结果；如果超时且最终尝试仍未成功，则返回 null。</returns>
    public static async ValueTask<TResult> WaitAsync<TState, TResult>(
        TState state,
        Func<TState, CancellationToken, ValueTask<TResult>> tryGetValue, 
        TimeoutValue timeout,
        TimeoutValue minSleepTime,
        TimeoutValue maxSleepTime,
        CancellationToken cancellationToken)
        where TResult : class
    {
        Invariant.Require(minSleepTime.CompareTo(maxSleepTime) <= 0);
        Invariant.Require(!maxSleepTime.IsInfinite);

        var initialResult = await tryGetValue(state, cancellationToken).ConfigureAwait(false);
        if (initialResult != null || timeout.IsZero)
        {
            return initialResult;
        }

        using var _ = CreateMergedCancellationTokenSource(timeout, cancellationToken, out var mergedCancellationToken);

        var random = new Random(Guid.NewGuid().GetHashCode());
        var sleepRangeMillis = maxSleepTime.InMilliseconds - minSleepTime.InMilliseconds;
        while (true)
        {
            var sleepTime = minSleepTime.TimeSpan + TimeSpan.FromMilliseconds(random.NextDouble() * sleepRangeMillis);
            try
            {
                await TaskHelper.Delay(sleepTime, mergedCancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (IsTimedOut())
            {
                // 如果在休眠期间超时，则使用常规令牌再尝试一次
                return await tryGetValue(state, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var result = await tryGetValue(state, mergedCancellationToken).ConfigureAwait(false);
                if (result != null) { return result; }
            }
            catch (OperationCanceledException) when (IsTimedOut())
            {
                return null;
            }
        }

        bool IsTimedOut() => 
            mergedCancellationToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
    }

    /// <summary>
    /// 创建一个合并了超时与取消令牌的 <see cref="CancellationTokenSource"/>。
    /// 当超时为无限时直接返回调用方提供的令牌；否则将超时合并到令牌源中。
    /// </summary>
    /// <param name="timeout">等待的总超时时间。</param>
    /// <param name="cancellationToken">调用方提供的取消令牌。</param>
    /// <param name="mergedCancellationToken">合并后的取消令牌。</param>
    /// <returns>用于释放合并令牌源的 <see cref="IDisposable"/>；当超时为无限时返回 null。</returns>
    private static IDisposable CreateMergedCancellationTokenSource(TimeoutValue timeout, CancellationToken cancellationToken, out CancellationToken mergedCancellationToken)
    {
        if (timeout.IsInfinite)
        {
            mergedCancellationToken = cancellationToken;
            return null;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            var timeoutSource = new CancellationTokenSource(millisecondsDelay: timeout.InMilliseconds);
            mergedCancellationToken = timeoutSource.Token;
            return timeoutSource;
        }

        var mergedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        mergedSource.CancelAfter(timeout.InMilliseconds);
        mergedCancellationToken = mergedSource.Token;
        return mergedSource;
    }
}