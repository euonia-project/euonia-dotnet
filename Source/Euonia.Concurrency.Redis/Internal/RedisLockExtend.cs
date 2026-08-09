using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using Nerosoft.Euonia.Threading.Redis;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// Implements the extend operation in the RedLock algorithm. See https://redis.io/topics/distlock
/// </summary>
internal readonly struct RedisLockExtend
{
    private readonly IRedisLockExtensibleSynchronizationPrimitive _primitive;
    private readonly Dictionary<IDatabase, Task<bool>> _tryAcquireOrRenewTasks;
    private readonly CancellationToken _cancellationToken;

    public RedisLockExtend(
        IRedisLockExtensibleSynchronizationPrimitive primitive, 
        Dictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks, 
        CancellationToken cancellationToken)
    {
        _primitive = primitive;
        _tryAcquireOrRenewTasks = tryAcquireOrRenewTasks;
        _cancellationToken = cancellationToken;
    }

    public async Task<bool?> TryExtendAsync()
    {
        Invariant.Require(!TaskHelper.IsSynchronous, "should only be called from a background renewal thread which is async");

        var incompleteTasks = new HashSet<Task>();
        foreach (var kvp in _tryAcquireOrRenewTasks.ToArray())
        {
            if (kvp.Value.IsCompleted)
            {
                incompleteTasks.Add(
                    _tryAcquireOrRenewTasks[kvp.Key] = Helpers.SafeCreateTask(
                        state => state.primitive.TryExtendAsync(state.database), 
                        (primitive: _primitive, database: kvp.Key)
                    )
                );
            }
            else
            {
                // if the previous acquire/renew is still going, just keep waiting for that
                incompleteTasks.Add(kvp.Value);
            }
        }

        // For extension we use the same timeout as acquire. This ensures the same min validity time which should be
        // sufficient to keep extending
        using var timeout = new TimeoutTask(_primitive.AcquireTimeout, _cancellationToken);
        incompleteTasks.Add(timeout.Task);

        var databaseCount = _tryAcquireOrRenewTasks.Count;
        var successCount = 0;
        var failCount = 0;
        while (true)
        {
            var completed = await Task.WhenAny(incompleteTasks).ConfigureAwait(false);

            if (completed == timeout.Task)
            {
                await completed.ConfigureAwait(false); // propagate cancellation
                return null; // inconclusive
            }

            if (completed.Status == TaskStatus.RanToCompletion && ((Task<bool>)completed).Result)
            {
                ++successCount;
                if (RedisLockHelper.HasSufficientSuccesses(successCount, databaseCount)) { return true; } 
            }
            else
            {
                // note that we treat faulted and failed the same in extend. There's no reason to throw, since
                // this is just called by the extend loop. While in theory a fault could indicate some kind of post-success
                // failure, most likely it means the db is unreachable and so it is safest to consider it a failure
                ++failCount;
                if (RedisLockHelper.HasTooManyFailuresOrFaults(failCount, databaseCount)) { return false; }
            }

            incompleteTasks.Remove(completed);
        }
    }
}