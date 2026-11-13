using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 读写锁的读锁原语：读者以集合成员的形式登记在同一个 Redis 键中，
/// 获取、续期与释放均通过 Lua 脚本在服务端原子完成。
/// </summary>
internal class RedisReadLockPrimitive : IRedisLockAcquirableSynchronizationPrimitive, IRedisLockExtensibleSynchronizationPrimitive
{
    private readonly RedisValue _lockId = RedisLockHelper.CreateLockId();
    private readonly RedisKey _readerKey, _writerKey;
    private readonly RedisLockTimeouts _timeouts;

    /// <summary>
    /// 初始化读锁原语。
    /// </summary>
    /// <param name="readerKey">存储读者集合的 Redis 键。</param>
    /// <param name="writerKey">表示写锁持有者或写者等待状态的 Redis 键。</param>
    /// <param name="timeouts">锁的过期时间与获取超时设置。</param>
    public RedisReadLockPrimitive(RedisKey readerKey, RedisKey writerKey, RedisLockTimeouts timeouts)
    {
        _readerKey = readerKey;
        _writerKey = writerKey;
        _timeouts = timeouts;
    }

    /// <inheritdoc />
    public TimeoutValue AcquireTimeout => _timeouts.AcquireTimeout;

    /// <summary>
    /// 释放读锁
    /// 
    /// 只需从读者集合中移除我们的 ID（如果它不在其中或集合不存在则不做任何操作）
    /// </summary>
    private static readonly RedisScript<RedisReadLockPrimitive> _releaseReadScript = new(
        @"redis.call('srem', @readerKey, @lockId)",
        p => new { readerKey = p._readerKey, lockId = p._lockId }
    );

    /// <inheritdoc />
    public void Release(IDatabase database, bool fireAndForget) => _releaseReadScript.Execute(database, this, fireAndForget);

    /// <inheritdoc />
    public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) => _releaseReadScript.ExecuteAsync(database, this, fireAndForget);

    /// <summary>
    /// 尝试续期读锁
    /// 
    /// 首先，检查读者集合是否存在以及我们的 ID 是否仍是其成员。如果不是，则失败。
    /// 
    /// 然后，将读者集合的 TTL 延长至至少我们的过期时间（之所以说至少，是因为其他读者可能以更长的过期时间在操作）
    /// </summary>
    private static readonly RedisScript<RedisReadLockPrimitive> _tryExtendReadScript = new(@"
            if redis.call('sismember', @readerKey, @lockId) == 0 then
                return 0
            end
            if redis.call('pttl', @readerKey) < tonumber(@expiryMillis) then
                redis.call('pexpire', @readerKey, @expiryMillis)
            end
            return 1",
        p => new { readerKey = p._readerKey, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
    );

    /// <inheritdoc />
    public Task<bool> TryExtendAsync(IDatabaseAsync database) => _tryExtendReadScript.ExecuteAsync(database, this).AsBooleanTask();

    /// <summary>
    /// 尝试获取读锁
    /// 
    /// 首先，检查写锁值：如果它存在，则失败。
    /// 
    /// 然后，将我们的 ID 添加到读者集合，若集合不存在则创建它。接着，将读者集合的
    /// TTL 延长至至少我们的过期时间。返回成功。
    /// </summary>
    private static readonly RedisScript<RedisReadLockPrimitive> _tryAcquireReadScript = new($@"
            if redis.call('exists', @writerKey) == 1 then
                return 0
            end
            redis.call('sadd', @readerKey, @lockId)
            local readerTtl = redis.call('pttl', @readerKey)
            if readerTtl < tonumber(@expiryMillis) then
                redis.call('pexpire', @readerKey, @expiryMillis)
            end
            return 1",
        p => new { writerKey = p._writerKey, readerKey = p._readerKey, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
    );

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(IDatabaseAsync database) => _tryAcquireReadScript.ExecuteAsync(database, this).AsBooleanTask();

    /// <inheritdoc />
    public bool TryAcquire(IDatabase database) => (bool)_tryAcquireReadScript.Execute(database, this);
}

/// <summary>
/// 写者等待原语：以带后缀的锁标识占用写锁键，表示已有写者排队，
/// 从而在不真正持有写锁的情况下阻止新的读者进入。
/// </summary>
internal class RedisWriterWaitingPrimitive : RedisMutexPrimitive
{
    /// <summary>
    /// 追加到锁标识后的后缀，用于区分“写者正在等待”与“写者已持有”两种状态。
    /// </summary>
    public const string LockIdSuffix = "_WRITERWAITING";

    /// <summary>
    /// 基于写锁键与基础锁标识初始化写者等待原语，实际使用的锁标识会附加 <see cref="LockIdSuffix"/>。
    /// </summary>
    /// <param name="writerKey">表示写锁状态或写者等待状态的 Redis 键。</param>
    /// <param name="baseLockId">当前持有者的基础锁标识。</param>
    /// <param name="timeouts">锁的过期时间与获取超时设置。</param>
    public RedisWriterWaitingPrimitive(RedisKey writerKey, RedisValue baseLockId, RedisLockTimeouts timeouts)
        : base(writerKey, baseLockId + LockIdSuffix, timeouts)
    {
    }
}

/// <summary>
/// 写锁原语：仅当写锁键被本方占用且读者集合为空时才获取成功，
/// 释放与续期委托给内部的互斥锁原语完成。
/// </summary>
internal class RedisWriteLockPrimitive : IRedisLockAcquirableSynchronizationPrimitive, IRedisLockExtensibleSynchronizationPrimitive
{
    private readonly RedisKey _readerKey, _writerKey;
    private readonly RedisValue _lockId;
    private readonly RedisLockTimeouts _timeouts;
    private readonly RedisMutexPrimitive _mutexPrimitive;

    /// <summary>
    /// 初始化写锁原语。
    /// </summary>
    /// <param name="readerKey">存储读者集合的 Redis 键，用于判断是否仍有读者持有读锁。</param>
    /// <param name="writerKey">表示写锁状态的 Redis 键。</param>
    /// <param name="lockId">当前持有者的唯一锁标识。</param>
    /// <param name="timeouts">锁的过期时间与获取超时设置。</param>
    public RedisWriteLockPrimitive(
        RedisKey readerKey, 
        RedisKey writerKey, 
        RedisValue lockId,
        RedisLockTimeouts timeouts)
    {
        _readerKey = readerKey;
        _writerKey = writerKey;
        _lockId = lockId;
        _timeouts = timeouts;
        _mutexPrimitive = new RedisMutexPrimitive(_writerKey, _lockId, _timeouts);
    }

    /// <inheritdoc />
    public TimeoutValue AcquireTimeout => _timeouts.AcquireTimeout;

    /// <inheritdoc />
    public void Release(IDatabase database, bool fireAndForget) => _mutexPrimitive.Release(database, fireAndForget);

    /// <inheritdoc />
    public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) => _mutexPrimitive.ReleaseAsync(database, fireAndForget);

    /// <summary>
    /// 尝试获取写锁
    /// 
    /// 首先，检查 writerValue 是否存在。如果存在，则失败，除非它是我们的等待 ID。
    /// 
    /// 然后，检查是否没有读者。如果没有，则将 writerValue 设置为我们的 ID 并返回成功。否则，如果锁
    /// 持有我们的等待 ID，则重新设置过期时间（避免需要延长写者等待锁）。
    /// 
    /// 最后，返回失败。
    /// </summary>
    private static readonly RedisScript<RedisWriteLockPrimitive> _tryAcquireWriteScript = new($@"
            local writerValue = redis.call('get', @writerKey)
            if writerValue == false or writerValue == @lockId .. '{RedisWriterWaitingPrimitive.LockIdSuffix}' then
                if redis.call('scard', @readerKey) == 0 then
                    redis.call('set', @writerKey, @lockId, 'px', @expiryMillis)
                    return 1
                end
                if writerValue ~= false then
                    redis.call('pexpire', @writerKey, @expiryMillis)
                end
            end
            return 0",
        p => new { writerKey = p._writerKey, readerKey = p._readerKey, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
    );

    /// <inheritdoc />
    public bool TryAcquire(IDatabase database) => (bool)_tryAcquireWriteScript.Execute(database, this);

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(IDatabaseAsync database) => _tryAcquireWriteScript.ExecuteAsync(database, this).AsBooleanTask();

    /// <inheritdoc />
    public Task<bool> TryExtendAsync(IDatabaseAsync database) => _mutexPrimitive.TryExtendAsync(database);
}