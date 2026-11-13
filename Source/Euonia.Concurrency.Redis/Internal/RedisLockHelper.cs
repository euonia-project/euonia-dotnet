using System.Diagnostics;
using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// RedLock 算法的通用辅助逻辑，包括多数派判定、锁标识生成、命令标志构造与发后即忘释放。
/// </summary>
internal static class RedisLockHelper
{
    private static readonly string _lockIdPrefix;

    static RedisLockHelper()
    {
        using var currentProcess = Process.GetCurrentProcess();
        _lockIdPrefix = $"{Environment.MachineName}_{currentProcess.Id}_";
    }

    /// <summary>
    /// 判断成功数量是否已达到数据库总数所需的多数派。
    /// </summary>
    /// <param name="successCount">已成功获取或续期的数据库数量。</param>
    /// <param name="databaseCount">参与投票的数据库总数。</param>
    /// <returns>成功数量达到多数派阈值时为 <see langword="true"/>。</returns>
    /// <remarks>调用方需保证 <paramref name="successCount"/> 不超过阈值，否则会触发不变量校验。</remarks>
    public static bool HasSufficientSuccesses(int successCount, int databaseCount)
    {
        // 需要多数派
        var threshold = (databaseCount / 2) + 1;
        // 虽然理论上只有在数量超过足够值时才会返回 true，但由于我们的实现方式，
        // 我们预期它只会在刚好足够或不够时被调用。
        Invariant.Require(successCount <= threshold);
        return successCount >= threshold;
    }

    /// <summary>
    /// 判断失败或出错的数据库数量是否已使多数派成功变得不可能。
    /// </summary>
    /// <param name="failureOrFaultCount">失败或出错的数据库数量。</param>
    /// <param name="databaseCount">参与投票的数据库总数。</param>
    /// <returns>失败或出错数量达到阻断阈值时为 <see langword="true"/>。</returns>
    /// <remarks>调用方需保证 <paramref name="failureOrFaultCount"/> 不超过阈值，否则会触发不变量校验。</remarks>
    public static bool HasTooManyFailuresOrFaults(int failureOrFaultCount, int databaseCount)
    {
        // 对于奇数个数据库，需要多数派才能使成功变得不可能。而对于
        // 偶数个数据库，达到 50% 的失败/错误就足以排除获得多数派成功的可能。
        var threshold = (databaseCount / 2) + (databaseCount % 2);
        // 虽然理论上只有在数量超过足够值时才会返回 true，但由于我们的实现方式，
        // 我们预期它只会在刚好足够或不够时被调用。
        Invariant.Require(failureOrFaultCount <= threshold);
        return failureOrFaultCount >= threshold;
    }

    /// <summary>
    /// 生成全局唯一的锁标识，由机器名、当前进程 ID 与 GUID 拼接而成。
    /// </summary>
    /// <returns>可用于在 Redis 中区分不同持有者的锁标识。</returns>
    public static RedisValue CreateLockId() => _lockIdPrefix + Guid.NewGuid().ToString("n");

    /// <summary>
    /// 判断任务是否已正常完成但返回 <see langword="false"/>，即明确表示获取或续期失败。
    /// </summary>
    /// <param name="task">待检查的获取或续期任务。</param>
    /// <returns>任务正常完成且结果为 <see langword="false"/> 时为 <see langword="true"/>。</returns>
    public static bool ReturnedFalse(Task<bool> task) => task.Status == TaskStatus.RanToCompletion && !task.Result;

    /// <summary>
    /// 在尚未完成的获取或续期任务结束后，以后台方式释放对应的锁，避免遗留未释放的键。
    /// </summary>
    /// <param name="primitive">执行释放操作的同步原语。</param>
    /// <param name="database">任务所对应的 Redis 数据库。</param>
    /// <param name="acquireOrRenewTask">尚未完成的获取或续期任务。</param>
    public static void FireAndForgetReleaseUponCompletion(IRedisLockReleasableSynchronizationPrimitive primitive, IDatabase database, Task<bool> acquireOrRenewTask)
    {
        if (ReturnedFalse(acquireOrRenewTask))
        {
            return;
        }

        acquireOrRenewTask.ContinueWith(async (t, state) =>
            {
                // 如果已知失败，就不要清理
                if (!ReturnedFalse(t) && state is IDatabase database)
                {
                    await primitive.ReleaseAsync(database, fireAndForget: true).ConfigureAwait(false);
                }
            },
            state: database
        );
    }

    /// <summary>
    /// 构造锁命令使用的标志：始终要求主节点，并按需附加发后即忘标志。
    /// </summary>
    /// <param name="fireAndForget">是否以不等待结果的方式发送命令。</param>
    /// <returns>包含 <see cref="CommandFlags.DemandMaster"/> 及可选 <see cref="CommandFlags.FireAndForget"/> 的标志组合。</returns>
    public static CommandFlags GetCommandFlags(bool fireAndForget) =>
        CommandFlags.DemandMaster | (fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None);

    /// <summary>
    /// 等待 Redis 脚本返回结果，并将其转换为布尔值。
    /// </summary>
    /// <param name="redisResultTask">Redis 脚本执行任务。</param>
    /// <returns>脚本返回值的布尔表示。</returns>
    public static async Task<bool> AsBooleanTask(this Task<RedisResult> redisResultTask) => (bool)await redisResultTask.ConfigureAwait(false);
}