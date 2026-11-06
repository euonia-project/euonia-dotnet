using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 信号量算法与互斥锁实现类似，区别在于存储在键处的值是一个
/// 有序集合（按超时时间排序）。由于元素超时后不会自动从集合中移除，
/// 想要获取信号量的调用方必须先清除集合中所有已过期的值，然后才能检查集合是否有
/// 可供它们使用的空间。
/// </summary>
internal class RedisSemaphorePrimitive : IRedisLockAcquirableSynchronizationPrimitive, IRedisLockExtensibleSynchronizationPrimitive
{
    // 在调用非确定性函数之前必须先调用 replicate_commands
    private const string GET_NOW_MILLIS_SCRIPT_FRAGMENT = @"
            redis.replicate_commands()
            local nowResult = redis.call('time')
            local nowMillis = (tonumber(nowResult[1]) * 1000.0) + (tonumber(nowResult[2]) / 1000.0)";

    private const string RENEW_SET_SCRIPT_FRAGMENT = @"
            local keyTtl = redis.call('pttl', @key)
            if keyTtl < tonumber(@setExpiryMillis) then
                redis.call('pexpire', @key, @setExpiryMillis)
            end";

    private readonly RedisValue _lockId = RedisLockHelper.CreateLockId();
    private readonly RedisKey _key;
    private readonly int _maxCount;
    private readonly RedisLockTimeouts _timeouts;

    /// <summary>
    /// 初始化信号量原语。
    /// </summary>
    /// <param name="key">存储信号量成员有序集合的 Redis 键。</param>
    /// <param name="maxCount">允许同时持有信号量的最大数量。</param>
    /// <param name="timeouts">成员的过期时间与获取超时设置。</param>
    public RedisSemaphorePrimitive(RedisKey key, int maxCount, RedisLockTimeouts timeouts)
    {
        _key = key;
        _maxCount = maxCount;
        _timeouts = timeouts;
    }

    /// <inheritdoc />
    public TimeoutValue AcquireTimeout => _timeouts.AcquireTimeout;

    /// <summary>
    /// 实际的过期时间由超时集合中的条目决定。不过，我们也不想让这些
    /// 集合永远留存而污染数据库。因此，我们为集合设置的过期时间是个体条目过期时间的 3 倍。对集合
    /// 格外保守的原因是，丢失集合造成的破坏比单个键超时更大。
    /// </summary>
    private TimeoutValue SetExpiry => TimeSpan.FromMilliseconds((int)Math.Min(int.MaxValue, 3L * _timeouts.Expiry.InMilliseconds));

    /// <inheritdoc />
    public void Release(IDatabase database, bool fireAndForget) =>
        database.SortedSetRemove(_key, _lockId, RedisLockHelper.GetCommandFlags(fireAndForget));

    /// <inheritdoc />
    public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) =>
        database.SortedSetRemoveAsync(_key, _lockId, RedisLockHelper.GetCommandFlags(fireAndForget));

    private static readonly RedisScript<RedisSemaphorePrimitive> _acquireScript = new($@"
            {GET_NOW_MILLIS_SCRIPT_FRAGMENT}
            redis.call('zremrangebyscore', @key, '-inf', nowMillis)
            if redis.call('zcard', @key) < tonumber(@maxCount) then
                redis.call('zadd', @key, nowMillis + tonumber(@expiryMillis), @lockId)
                {RENEW_SET_SCRIPT_FRAGMENT}
                return 1
            end
            return 0",
        p => new { key = p._key, maxCount = p._maxCount, expiryMillis = p._timeouts.Expiry.InMilliseconds, lockId = p._lockId, setExpiryMillis = p.SetExpiry.InMilliseconds }
    );

    /// <inheritdoc />
    public bool TryAcquire(IDatabase database) => (bool)_acquireScript.Execute(database, this);

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(IDatabaseAsync database) => _acquireScript.ExecuteAsync(database, this).AsBooleanTask();

    private static readonly RedisScript<RedisSemaphorePrimitive> _extendScript = new($@"
            {GET_NOW_MILLIS_SCRIPT_FRAGMENT}
            local result = redis.call('zadd', @key, 'XX', 'CH', nowMillis + tonumber(@expiryMillis), @lockId)
            {RENEW_SET_SCRIPT_FRAGMENT}
            return result",
        p => new { key = p._key, expiryMillis = p._timeouts.Expiry.InMilliseconds, lockId = p._lockId, setExpiryMillis = p.SetExpiry.InMilliseconds }
    );

    /// <inheritdoc />
    public Task<bool> TryExtendAsync(IDatabaseAsync database) => _extendScript.ExecuteAsync(database, this).AsBooleanTask();
}