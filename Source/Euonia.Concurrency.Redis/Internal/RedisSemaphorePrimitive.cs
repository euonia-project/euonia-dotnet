using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// The semaphore algorithm looks similar to the mutex implementation except that the value stored at the key is a
/// sorted set (sorted by timeout). Because elements aren't automatically removed from the set when they time out,
/// would-be acquirers must first purge the set of any expired values before they check whether the set has space
/// for them.
/// </summary>
internal class RedisSemaphorePrimitive : IRedisLockAcquirableSynchronizationPrimitive, IRedisLockExtensibleSynchronizationPrimitive
{
    // replicate_commands is necessary to call before calling non-deterministic functions
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

    public RedisSemaphorePrimitive(RedisKey key, int maxCount, RedisLockTimeouts timeouts)
    {
        _key = key;
        _maxCount = maxCount;
        _timeouts = timeouts;
    }

    public TimeoutValue AcquireTimeout => _timeouts.AcquireTimeout;

    /// <summary>
    /// The actual expiry is determined by the entry in the timeouts set. However, we also don't want to pollute the db by leaving
    /// the sets around forever. Therefore, we give the sets an expiry of 3x the individual entry expiry. The reason to be extra
    /// conservative with sets is that there is more disruption from losing them then from having one key time out.
    /// </summary>
    private TimeoutValue SetExpiry => TimeSpan.FromMilliseconds((int)Math.Min(int.MaxValue, 3L * _timeouts.Expiry.InMilliseconds));

    public void Release(IDatabase database, bool fireAndForget) =>
        database.SortedSetRemove(_key, _lockId, RedisLockHelper.GetCommandFlags(fireAndForget));

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

    public bool TryAcquire(IDatabase database) => (bool)_acquireScript.Execute(database, this);

    public Task<bool> TryAcquireAsync(IDatabaseAsync database) => _acquireScript.ExecuteAsync(database, this).AsBooleanTask();

    private static readonly RedisScript<RedisSemaphorePrimitive> _extendScript = new($@"
            {GET_NOW_MILLIS_SCRIPT_FRAGMENT}
            local result = redis.call('zadd', @key, 'XX', 'CH', nowMillis + tonumber(@expiryMillis), @lockId)
            {RENEW_SET_SCRIPT_FRAGMENT}
            return result",
        p => new { key = p._key, expiryMillis = p._timeouts.Expiry.InMilliseconds, lockId = p._lockId, setExpiryMillis = p.SetExpiry.InMilliseconds }
    );

    public Task<bool> TryExtendAsync(IDatabaseAsync database) => _extendScript.ExecuteAsync(database, this).AsBooleanTask();
}