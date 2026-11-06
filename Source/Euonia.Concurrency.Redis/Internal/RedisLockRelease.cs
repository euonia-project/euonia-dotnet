using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 实现 RedLock 算法中的释放操作。参见 https://redis.io/topics/distlock
/// </summary>
internal readonly struct RedisLockRelease
{
    private readonly IRedisLockReleasableSynchronizationPrimitive _primitive;
    private readonly IReadOnlyDictionary<IDatabase, Task<bool>> _tryAcquireOrRenewTasks;

    /// <summary>
    /// 初始化 RedLock 释放操作。
    /// </summary>
    /// <param name="primitive">在各数据库上执行释放的同步原语。</param>
    /// <param name="tryAcquireOrRenewTasks">获取或最近一次续期产生的任务字典，用于判断各数据库是否确实持有锁。</param>
    public RedisLockRelease(IRedisLockReleasableSynchronizationPrimitive primitive, IReadOnlyDictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks)
    {
        _primitive = primitive;
        _tryAcquireOrRenewTasks = tryAcquireOrRenewTasks;
    }

    /// <summary>
    /// 在所有已获取的数据库上释放锁，并在多数派已释放或已无法继续时提前结束；剩余数据库改为后台释放。
    /// </summary>
    /// <returns>表示异步释放操作的值任务。</returns>
    /// <exception cref="AggregateException">释放过程中出错的数据库过多，已无法完成多数派释放。</exception>
    public async ValueTask ReleaseAsync()
    {
        var isSynchronous = TaskHelper.IsSynchronous;
        var unreleasedTryAcquireOrRenewTasks = _tryAcquireOrRenewTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        List<Exception>? releaseExceptions = null;
        var successCount = 0;
        var faultCount = 0;
        var databaseCount = unreleasedTryAcquireOrRenewTasks.Count;

        try
        {
            while (true)
            {
                var databases = unreleasedTryAcquireOrRenewTasks.Where(kvp => kvp.Value.IsCompleted)
                                                                // 先处理未出错的任务
                                                                .OrderByDescending(kvp => kvp.Value.Status == TaskStatus.RanToCompletion)
                                                                // 然后从失败的任务开始，因为无需对它们执行释放操作
                                                                .ThenBy(kvp => kvp.Value.Status == TaskStatus.RanToCompletion && kvp.Value.Result)
                                                                .Select(kvp => kvp.Key)
                                                                .ToArray();
                foreach (var db in databases)
                {
                    var tryAcquireOrRenewTask = unreleasedTryAcquireOrRenewTasks[db];
                    unreleasedTryAcquireOrRenewTasks.Remove(db);

                    if (RedisLockHelper.ReturnedFalse(tryAcquireOrRenewTask))
                    {
                        // 如果获取失败，则无需释放
                        ++successCount;
                    }
                    else
                    {
                        try
                        {
                            if (isSynchronous)
                            {
                                _primitive.Release(db, fireAndForget: false);
                            }
                            else
                            {
                                await _primitive.ReleaseAsync(db, fireAndForget: false).ConfigureAwait(false);
                            }

                            ++successCount;
                        }
                        catch (Exception ex)
                        {
                            (releaseExceptions ??= new List<Exception>()).Add(ex);
                            ++faultCount;
                            if (RedisLockHelper.HasTooManyFailuresOrFaults(faultCount, databaseCount))
                            {
                                throw new AggregateException(releaseExceptions).Flatten();
                            }
                        }
                    }

                    if (RedisLockHelper.HasSufficientSuccesses(successCount, databaseCount))
                    {
                        return;
                    }
                }

                // 如果尚未释放足够多的锁以完成操作或确定成功或失败，则等待另一个完成
                if (isSynchronous)
                {
                    // ReSharper disable once CoVariantArrayConversion
                    Task.WaitAny(unreleasedTryAcquireOrRenewTasks.Values.ToArray());
                }
                else
                {
                    await Task.WhenAny(unreleasedTryAcquireOrRenewTasks.Values).ConfigureAwait(false);
                }
            }
        }
        finally // 其余的发后即忘
        {
            foreach (var kvp in unreleasedTryAcquireOrRenewTasks)
            {
                RedisLockHelper.FireAndForgetReleaseUponCompletion(_primitive, kvp.Key, kvp.Value);
            }
        }
    }
}