using System.Diagnostics;
using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using Nerosoft.Euonia.Threading.Redis;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 实现 RedLock 算法中的获取操作。参见 https://redis.io/topics/distlock
/// </summary>
internal readonly struct RedisLockAcquire
{
    private readonly IRedisLockAcquirableSynchronizationPrimitive _primitive;
    private readonly IReadOnlyList<IDatabase> _databases;
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// 初始化 RedLock 获取操作。
    /// </summary>
    /// <param name="primitive">在各数据库上执行获取的同步原语。</param>
    /// <param name="databases">参与 RedLock 投票的 Redis 数据库集合，必须包含至少一个元素。</param>
    /// <param name="cancellationToken">用于中止整个获取过程的取消令牌。</param>
    public RedisLockAcquire(
        IRedisLockAcquirableSynchronizationPrimitive primitive,
        IReadOnlyList<IDatabase> databases,
        CancellationToken cancellationToken)
    {
        _primitive = primitive;
        _databases = databases;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// 在所有数据库上并发发起获取，并在达到多数派成功时判定获取成功。
    /// </summary>
    /// <returns>获取成功时返回各数据库对应的获取任务字典，供后续续期与释放复用；获取失败或超时时返回 <see langword="null"/>。</returns>
    /// <exception cref="OperationCanceledException">取消令牌已触发。</exception>
    /// <exception cref="AggregateException">返回错误或已取消的数据库过多，已无法达成多数派。</exception>
    public async ValueTask<Dictionary<IDatabase, Task<bool>>?> TryAcquireAsync()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var isSynchronous = TaskHelper.IsSynchronous;
        if (isSynchronous && _databases.Count == 1)
        {
            return TrySingleFullySynchronousAcquire();
        }

        var primitive = _primitive;
        var tryAcquireTasks = _databases.ToDictionary(
            db => db,
            db => Helpers.SafeCreateTask(state => state.primitive.TryAcquireAsync(state.db), (primitive, db))
        );

        var waitForAcquireTask = WaitForAcquireAsync(tryAcquireTasks);

        var succeeded = false;
        try
        {
            succeeded = await waitForAcquireTask.AwaitSyncOverAsync().ConfigureAwait(false);
        }
        finally
        {
            // 清理
            if (!succeeded)
            {
                List<Task>? releaseTasks = null;
                foreach (var kvp in tryAcquireTasks)
                {
                    // 如果任务尚未完成，我们此刻不想执行任何释放；只需
                    // 排队一个释放命令，待该任务最终完成时运行
                    if (!kvp.Value.IsCompleted)
                    {
                        RedisLockHelper.FireAndForgetReleaseUponCompletion(primitive, kvp.Key, kvp.Value);
                    }
                    // 否则，除非我们已知获取失败，否则执行释放
                    else if (!RedisLockHelper.ReturnedFalse(kvp.Value))
                    {
                        if (isSynchronous)
                        {
                            try
                            {
                                primitive.Release(kvp.Key, fireAndForget: true);
                            }
                            catch
                            {
                                // 忽略释放时的异常
                            }
                        }
                        else
                        {
                            (releaseTasks ??= new List<Task>())
                                .Add(Helpers.SafeCreateTask(state => state.primitive.ReleaseAsync(state.Key, fireAndForget: true), (primitive, kvp.Key)));
                        }
                    }
                }

                if (releaseTasks != null)
                {
                    await Task.WhenAll(releaseTasks).ConfigureAwait(false);
                }
            }
        }

        return succeeded ? tryAcquireTasks : null;
    }

    private async Task<bool> WaitForAcquireAsync(IReadOnlyDictionary<IDatabase, Task<bool>> tryAcquireTasks)
    {
        using var timeout = new TimeoutTask(_primitive.AcquireTimeout, _cancellationToken);
        var incompleteTasks = new HashSet<Task>(tryAcquireTasks.Values) { timeout.Task };

        var successCount = 0;
        var failCount = 0;
        var faultCount = 0;
        while (true)
        {
            var completed = await Task.WhenAny(incompleteTasks).ConfigureAwait(false);

            if (completed == timeout.Task)
            {
                await completed.ConfigureAwait(false); // 传播取消
                return false; // 真正的超时
            }

            if (completed.Status == TaskStatus.RanToCompletion)
            {
                var result = await ((Task<bool>)completed).ConfigureAwait(false);
                if (result)
                {
                    ++successCount;
                    if (RedisLockHelper.HasSufficientSuccesses(successCount, _databases.Count))
                    {
                        return true;
                    }
                }
                else
                {
                    ++failCount;
                    if (RedisLockHelper.HasTooManyFailuresOrFaults(failCount, _databases.Count))
                    {
                        return false;
                    }
                }
            }
            else // 出错或已取消
            {
                // 如果出现太多错误，则无法获取锁，因此应当抛出异常
                ++faultCount;
                if (RedisLockHelper.HasTooManyFailuresOrFaults(faultCount, _databases.Count))
                {
                    var faultingTasks = tryAcquireTasks.Values.Where(t => t.IsCanceled || t.IsFaulted)
                                                       .ToArray();
                    if (faultingTasks.Length == 1)
                    {
                        await faultingTasks[0].ConfigureAwait(false); // 传播错误
                    }

                    throw new AggregateException(faultingTasks.Select(t => t.Exception ?? global::Extensions.As<Exception>((object)new TaskCanceledException(t))))
                        .Flatten();
                }

                ++failCount;
                if (RedisLockHelper.HasTooManyFailuresOrFaults(failCount, _databases.Count))
                {
                    return false;
                }
            }

            incompleteTasks.Remove(completed);
            Invariant.Require(incompleteTasks.Count > 1, "should be more than just timeout left");
        }
    }

    /// <summary>
    /// 我们仅允许对单个数据库执行同步获取，因为 StackExchange.Redis 目前不支持
    /// 单操作超时/取消。因此，一次缓慢的响应就会危及我们按时声明锁的能力。
    /// 在单数据库场景下，这一次操作就是全部关键所在，所以即使需要等待也可以接受。
    /// </summary>
    private Dictionary<IDatabase, Task<bool>>? TrySingleFullySynchronousAcquire()
    {
        var database = _databases.Single();

        bool success;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            success = _primitive.TryAcquire(database);
        }
        catch
        {
            // 失败时仍尝试释放，以防万一
            try
            {
                _primitive.Release(database, fireAndForget: true);
            }
            catch
            {
                // 忽略释放时的异常
            } // 什么都不做；反正我们即将抛出异常，且失败原因很可能相同

            throw;
        }

        if (success)
        {
            // 确保我们没有超时
            if (_primitive.AcquireTimeout.CompareTo(stopwatch.Elapsed) >= 0)
            {
                return new Dictionary<IDatabase, Task<bool>> { [database] = Task.FromResult(true) };
            }

            _primitive.Release(database, fireAndForget: true); // 已超时，因此释放
        }
        {}
        return null;
    }
}