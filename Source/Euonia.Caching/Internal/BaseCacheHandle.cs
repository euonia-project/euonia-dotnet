namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// <c>BaseCacheHandle</c> 实现了所有缓存句柄可能共用的逻辑。它抽象了 <see cref="ICache{T}"/> 接口，
/// 并定义了实现方必须使用的属性和方法。
/// <para>实际上，不建议绕过 <see cref="BaseCacheHandle{T}"/> 直接实现。</para>
/// </summary>
/// <typeparam name="TCacheValue">缓存值的类型。</typeparam>
public abstract class BaseCacheHandle<TCacheValue> : BaseCache<TCacheValue>
{
    private readonly object _updateLock = new();

    /// <summary>
    /// 按前缀移除缓存项；默认实现不支持并返回 0。
    /// </summary>
    /// <param name="prefix">键前缀。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>移除的条目数。</returns>
    public virtual Task<long> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }

    /// <summary>
    /// 初始化 <see cref="BaseCacheHandle{TCacheValue}"/> 类的新实例。
    /// </summary>
    /// <param name="managerConfiguration">管理器的配置。</param>
    /// <param name="configuration">配置。</param>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="managerConfiguration"/> 或 <paramref name="configuration"/> 为 null。
    /// </exception>
    /// <exception cref="ArgumentException">如果 <paramref name="configuration"/> 的名称为空。</exception>
    protected BaseCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        Check.EnsureNotNull(managerConfiguration, nameof(managerConfiguration));
        Check.EnsureNotNullOrWhiteSpace(configuration.Name, nameof(configuration.Name));

        Configuration = configuration;

        Stats = new CacheStats<TCacheValue>(managerConfiguration.Name, Configuration.Name, Configuration.EnableStatistics);
    }

    /// <summary>
    /// 指示此缓存句柄是否为分布式缓存。
    /// </summary>
    /// <remarks>
    /// 该值将由背板逻辑评估，以确定在收到远程事件时应执行的操作。
    /// <para>
    /// 如果缓存句柄是分布式的，例如远程移除事件不会引发另一次 <c>Remove</c> 调用。
    /// 但对于作为背板源的内存缓存句柄，它会触发一次 <c>Remove</c>。
    /// </para>
    /// </remarks>
    public virtual bool IsDistributedCache { get { return false; } }

    /// <summary>
    /// 当底层缓存因容量或过期而逐出某个项时触发，供管理器同步统计与背板状态。
    /// </summary>
    internal event EventHandler<CacheItemRemovedEventArgs>? OnCacheSpecificRemove;

    /// <summary>
    /// 获取缓存句柄配置。
    /// </summary>
    /// <value>配置。</value>
    public CacheHandleConfiguration Configuration { get; }

    /// <summary>
    /// 获取缓存句柄当前维护的项数。
    /// </summary>
    /// <value>计数。</value>
    public abstract int Count { get; }

    /// <summary>
    /// 获取缓存统计对象。
    /// </summary>
    /// <value>统计数据。</value>
    public virtual CacheStats<TCacheValue> Stats { get; }

    /// <summary>
    /// 更新缓存中已存在的键。
    /// <para>
    /// 缓存管理器将确保更新始终基于最新的版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，且在更新过程中其他客户端更改了
    /// 该键的值，缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="updateValue">用于执行更新的函数。</param>
    /// <param name="maxRetries">重试次数。</param>
    /// <returns>由缓存管理器解释的更新结果。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，则 Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    public virtual CacheItemUpdateResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
    {
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        CheckDisposed();

        lock (_updateLock)
        {
            var original = GetCacheItem(key);
            if (original == null)
            {
                return CacheItemUpdateResult.ForItemDidNotExist<TCacheValue>();
            }

            var newValue = updateValue(original.Value);

            if (newValue == null)
            {
                return CacheItemUpdateResult.ForFactoryReturnedNull<TCacheValue>();
            }

            var newItem = original.WithValue(newValue);
            newItem.LastAccessedUtc = DateTime.UtcNow;
            Put(newItem);
            return CacheItemUpdateResult.ForSuccess(newItem);
        }
    }

    /// <summary>
    /// 更新缓存中已存在的键。
    /// <para>
    /// 缓存管理器将确保更新始终基于最新的版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，且在更新过程中其他客户端更改了
    /// 该键的值，缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="updateValue">用于执行更新的函数。</param>
    /// <param name="maxRetries">重试次数。</param>
    /// <returns>由缓存管理器解释的更新结果。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，则 Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    public virtual CacheItemUpdateResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
    {
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        CheckDisposed();

        lock (_updateLock)
        {
            var original = GetCacheItem(key, region);
            if (original == null)
            {
                return CacheItemUpdateResult.ForItemDidNotExist<TCacheValue>();
            }

            var newValue = updateValue(original.Value);
            if (newValue == null)
            {
                return CacheItemUpdateResult.ForFactoryReturnedNull<TCacheValue>();
            }

            var newItem = original.WithValue(newValue);

            newItem.LastAccessedUtc = DateTime.UtcNow;
            Put(newItem);
            return CacheItemUpdateResult.ForSuccess(newItem);
        }
    }

    /// <summary>
    /// 向缓存中添加值。
    /// </summary>
    /// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
    /// <returns>
    /// 如果该键尚未添加到缓存则为 <c>true</c>，否则为 <c>false</c>。
    /// </returns>
    protected override bool AddInternal(CacheItem<TCacheValue> item)
    {
        CheckDisposed();
        item = GetItemExpiration(item);
        return AddInternalPrepared(item);
    }

    /// <summary>
    /// 将 <paramref name="item"/> 放入缓存。如果项已存在，将使用新值更新；如果项不存在，
    /// 则将其添加到缓存。
    /// </summary>
    /// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
    protected override void PutInternal(CacheItem<TCacheValue> item)
    {
        CheckDisposed();
        item = GetItemExpiration(item);
        PutInternalPrepared(item);
    }

    /// <summary>
    /// 当底层缓存支持且实现能够响应缓存项的逐出和过期时，可用于向
    /// <see cref="ICacheManager{TCacheValue}"/> 发出移除事件信号。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。可以为 null。</param>
    /// <param name="reason">原因。</param>
    /// <param name="value">原始缓存值。如果底层缓存系统不支持在逐出时返回值，则该值可能为 null。</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="key"/> 为 null。</exception>
    protected void TriggerCacheSpecificRemove(string key, string? region, CacheItemRemovedReason reason, object? value)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

        // 内部移除事件，此时尚不知道层级 => 发出 0
        OnCacheSpecificRemove?.Invoke(this, new CacheItemRemovedEventArgs(key, region, reason, value));
    }

    /// <summary>
    /// 向缓存中添加值。
    /// </summary>
    /// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
    /// <returns>
    /// 如果该键尚未添加到缓存则为 <c>true</c>，否则为 <c>false</c>。
    /// </returns>
    protected abstract bool AddInternalPrepared(CacheItem<TCacheValue> item);

    /// <summary>
    /// 执行与释放或重置非托管资源关联的应用程序定义任务。
    /// </summary>
    /// <param name="disposeManaged">指示是否应释放托管资源。</param>
    protected override void Dispose(bool disposeManaged)
    {
        if (disposeManaged)
        {
            Stats.Dispose();
        }

        base.Dispose(disposeManaged);
    }

    /// <summary>
    /// 获取项的过期设置。
    /// </summary>
    /// <param name="item">项。</param>
    /// <returns>返回更新后的缓存项。</returns>
    /// <exception cref="ArgumentNullException">如果 item 为 null。</exception>
    /// <exception cref="InvalidOperationException">
    /// 如果在未定义超时的情况下定义了过期模式。
    /// </exception>
    protected virtual CacheItem<TCacheValue> GetItemExpiration(CacheItem<TCacheValue> item)
    {
        Check.EnsureNotNull(item, nameof(item));

        // 逻辑应当是：项设置优先于句柄设置。如果项未定义模式（值为 Default），
        // 则应使用句柄的设置；如果句柄也未定义模式（值为 None|Default），则使用 None。
        var expirationMode = CacheExpirationMode.Default;
        var expirationTimeout = TimeSpan.Zero;
        var useItemExpiration = item.ExpirationMode != CacheExpirationMode.Default && !item.UsesExpirationDefaults;

        if (useItemExpiration)
        {
            expirationMode = item.ExpirationMode;
            expirationTimeout = item.ExpirationTimeout;
        }
        else if (Configuration.ExpirationMode != CacheExpirationMode.Default)
        {
            expirationMode = Configuration.ExpirationMode;
            expirationTimeout = Configuration.ExpirationTimeout;
        }

        if (expirationMode == CacheExpirationMode.Default || expirationMode == CacheExpirationMode.None)
        {
            expirationMode = CacheExpirationMode.None;
            expirationTimeout = TimeSpan.Zero;
        }
        else if (expirationTimeout == TimeSpan.Zero)
        {
            throw new InvalidOperationException(Resources.IDS_EXPIRATION_MODE_WITHOUT_TIMEOUT);
        }

        return item.WithExpiration(expirationMode, expirationTimeout, !useItemExpiration);
    }

    /// <summary>
    /// 将 <paramref name="item"/> 放入缓存。如果项已存在，将使用新值更新；如果项不存在，
    /// 则将其添加到缓存。
    /// </summary>
    /// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
    protected abstract void PutInternalPrepared(CacheItem<TCacheValue> item);
}