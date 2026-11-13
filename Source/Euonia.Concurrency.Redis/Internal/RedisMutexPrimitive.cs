using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 互斥锁原语：借助单个 Redis 键的 SET NX 语义实现互斥，并通过 Lua 脚本校验持有者后释放或续期。
/// </summary>
internal class RedisMutexPrimitive : IRedisLockAcquirableSynchronizationPrimitive, IRedisLockExtensibleSynchronizationPrimitive
{
    private readonly RedisKey _key;
    private readonly RedisValue _lockId;
    private readonly RedisLockTimeouts _timeouts;

    /// <summary>
    /// 初始化互斥锁原语。
    /// </summary>
    /// <param name="key">表示锁的 Redis 键。</param>
    /// <param name="lockId">当前持有者的唯一锁标识。</param>
    /// <param name="timeouts">锁的过期时间与获取超时设置。</param>
    public RedisMutexPrimitive(RedisKey key, RedisValue lockId, RedisLockTimeouts timeouts)
    {
        _key = key;
        _lockId = lockId;
        _timeouts = timeouts;
    }

    /// <inheritdoc />
    public TimeoutValue AcquireTimeout => _timeouts.AcquireTimeout;

    private static readonly RedisScript<RedisMutexPrimitive> _releaseScript = new(@"
            if redis.call('get', @key) == @lockId then
                return redis.call('del', @key)
            end
            return 0",
        p => new { key = p._key, lockId = p._lockId }
    );

    /// <inheritdoc />
    public void Release(IDatabase database, bool fireAndForget) => _releaseScript.Execute(database, this, fireAndForget);

    /// <inheritdoc />
    public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) => _releaseScript.ExecuteAsync(database, this, fireAndForget);

    /// <inheritdoc />
    public bool TryAcquire(IDatabase database) =>
        database.StringSet(_key, _lockId, _timeouts.Expiry.TimeSpan, When.NotExists, CommandFlags.DemandMaster);

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(IDatabaseAsync database) =>
        database.StringSetAsync(_key, _lockId, _timeouts.Expiry.TimeSpan, When.NotExists, CommandFlags.DemandMaster);

    private static readonly RedisScript<RedisMutexPrimitive> _extendScript = new(@"
            if redis.call('get', @key) == @lockId then
                return redis.call('pexpire', @key, @expiryMillis)
            end
            return 0",
        p => new { key = p._key, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
    );

    /// <inheritdoc />
    public Task<bool> TryExtendAsync(IDatabaseAsync database) => _extendScript.ExecuteAsync(database, this).AsBooleanTask();
}