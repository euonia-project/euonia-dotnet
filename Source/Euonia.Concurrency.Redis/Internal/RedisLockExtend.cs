using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using Nerosoft.Euonia.Threading.Redis;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 实现 RedLock 算法中的续期操作。参见 https://redis.io/topics/distlock
/// </summary>
internal readonly struct RedisLockExtend
{
    private readonly IRedisLockExtensibleSynchronizationPrimitive _primitive;
    private readonly Dictionary<IDatabase, Task<bool>> _tryAcquireOrRenewTasks;
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// 初始化 RedLock 续期操作。
    /// </summary>
    /// <param name="primitive">在各数据库上执行续期的同步原语。</param>
    /// <param name="tryAcquireOrRenewTasks">获取或上次续期产生的任务字典，用于判断各数据库的当前状态。</param>
    /// <param name="cancellationToken">用于中止续期过程的取消令牌。</param>
    public RedisLockExtend(
        IRedisLockExtensibleSynchronizationPrimitive primitive, 
        Dictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks, 
        CancellationToken cancellationToken)
    {
        _primitive = primitive;
        _tryAcquireOrRenewTasks = tryAcquireOrRenewTasks;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// 在所有已获取的数据库上并发续期锁，并将错误与失败同等视为续期失败。
    /// </summary>
    /// <returns>多数派续期成功时为 <see langword="true"/>；多数派失败时为 <see langword="false"/>；超时导致结论不确定时为 <see langword="null"/>。</returns>
    /// <exception cref="OperationCanceledException">取消令牌已触发。</exception>
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
                // 如果上一次获取/续期仍在进行，就继续等待它
                incompleteTasks.Add(kvp.Value);
            }
        }

        // 续期使用与获取相同的超时时间。这可确保相同的最短有效期，应足以
        // 维持续期
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
                await completed.ConfigureAwait(false); // 传播取消
                return null; // 结论不确定
            }

            if (completed.Status == TaskStatus.RanToCompletion && ((Task<bool>)completed).Result)
            {
                ++successCount;
                if (RedisLockHelper.HasSufficientSuccesses(successCount, databaseCount)) { return true; } 
            }
            else
            {
                // 注意，在续期时我们将出错与失败同等对待。没有理由抛出异常，因为
                // 这只是由续期循环调用。虽然理论上错误可能表明某种成功后
                // 的故障，但大多数情况下它意味着数据库不可达，因此将其视为失败最为安全
                ++failCount;
                if (RedisLockHelper.HasTooManyFailuresOrFaults(failCount, databaseCount)) { return false; }
            }

            incompleteTasks.Remove(completed);
        }
    }
}