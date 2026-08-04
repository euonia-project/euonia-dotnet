using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// Implements the release operation in the RedLock algorithm. See https://redis.io/topics/distlock
/// </summary>
internal readonly struct RedisLockRelease
{
    private readonly IRedisLockReleasableSynchronizationPrimitive _primitive;
    private readonly IReadOnlyDictionary<IDatabase, Task<bool>> _tryAcquireOrRenewTasks;

    public RedisLockRelease(IRedisLockReleasableSynchronizationPrimitive primitive, IReadOnlyDictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks)
    {
        _primitive = primitive;
        _tryAcquireOrRenewTasks = tryAcquireOrRenewTasks;
    }

    public async ValueTask ReleaseAsync()
    {
        var isSynchronous = TaskHelper.IsSynchronous;
        var unreleasedTryAcquireOrRenewTasks = _tryAcquireOrRenewTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        List<Exception> releaseExceptions = null;
        var successCount = 0;
        var faultCount = 0;
        var databaseCount = unreleasedTryAcquireOrRenewTasks.Count;

        try
        {
            while (true)
            {
                var databases = unreleasedTryAcquireOrRenewTasks.Where(kvp => kvp.Value.IsCompleted)
                                                                // work through non-faulted tasks first
                                                                .OrderByDescending(kvp => kvp.Value.Status == TaskStatus.RanToCompletion)
                                                                // then start with failed since no action is required to release those
                                                                .ThenBy(kvp => kvp.Value.Status == TaskStatus.RanToCompletion && kvp.Value.Result)
                                                                .Select(kvp => kvp.Key)
                                                                .ToArray();
                foreach (var db in databases)
                {
                    var tryAcquireOrRenewTask = unreleasedTryAcquireOrRenewTasks[db];
                    unreleasedTryAcquireOrRenewTasks.Remove(db);

                    if (RedisLockHelper.ReturnedFalse(tryAcquireOrRenewTask))
                    {
                        // if we failed to acquire, we don't need to release
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
                                throw new AggregateException(releaseExceptions!).Flatten();
                            }
                        }
                    }

                    if (RedisLockHelper.HasSufficientSuccesses(successCount, databaseCount))
                    {
                        return;
                    }
                }

                // if we haven't released enough yet to be done or certain of success or failure, wait for another to finish
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
        finally // fire and forget the rest
        {
            foreach (var kvp in unreleasedTryAcquireOrRenewTasks)
            {
                RedisLockHelper.FireAndForgetReleaseUponCompletion(_primitive, kvp.Key, kvp.Value);
            }
        }
    }
}