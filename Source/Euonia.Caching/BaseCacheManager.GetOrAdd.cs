namespace Nerosoft.Euonia.Caching;

public partial class BaseCacheManager<TValue>
{
    /// <inheritdoc />
    public TValue GetOrAdd(string key, TValue value)
        => GetOrAdd(key, _ => value);

    /// <inheritdoc />
    public TValue GetOrAdd(string key, string region, TValue value)
        => GetOrAdd(key, region, (_, _) => value);

    /// <inheritdoc />
    public TValue GetOrAdd(string key, Func<string, TValue> valueFactory)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        return GetOrAddInternal(key, null, (k, _) => new CacheItem<TValue>(k, valueFactory(k))).Value;
    }

    /// <inheritdoc />
    public TValue GetOrAdd(string key, string region, Func<string, string, TValue> valueFactory)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        return GetOrAddInternal(key, region, (k, r) => new CacheItem<TValue>(k, r, valueFactory(k, r))).Value;
    }

    /// <inheritdoc />
    public CacheItem<TValue> GetOrAdd(string key, Func<string, CacheItem<TValue>> valueFactory)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        return GetOrAddInternal(key, null, (k, _) => valueFactory(k));
    }

    /// <inheritdoc />
    public CacheItem<TValue> GetOrAdd(string key, string region, Func<string, string, CacheItem<TValue>> valueFactory)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        return GetOrAddInternal(key, region, valueFactory);
    }

    /// <inheritdoc />
    public bool TryGetOrAdd(string key, Func<string, TValue> valueFactory, out TValue value)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        if (TryGetOrAddInternal(
            key,
            null,
            (k, _) =>
            {
                var newValue = valueFactory(k);
                return newValue == null ? null : new CacheItem<TValue>(k, newValue);
            },
            out var item))
        {
            value = item.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetOrAdd(string key, string region, Func<string, string, TValue> valueFactory, out TValue value)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        if (TryGetOrAddInternal(
            key,
            region,
            (k, r) =>
            {
                var newValue = valueFactory(k, r);
                return newValue == null ? null : new CacheItem<TValue>(k, r, newValue);
            },
            out var item))
        {
            value = item.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetOrAdd(string key, Func<string, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        return TryGetOrAddInternal(key, null, (k, _) => valueFactory(k), out item);
    }

    /// <inheritdoc />
    public bool TryGetOrAdd(string key, string region, Func<string, string, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
        Check.EnsureNotNull(valueFactory, nameof(valueFactory));

        return TryGetOrAddInternal(key, region, valueFactory, out item);
    }

    /// <summary>
    /// 尝试获取指定键的缓存项；若不存在，则通过值工厂创建并添加。返回是否成功获取到缓存项。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
    /// <param name="valueFactory">用于创建缓存项的值工厂函数。</param>
    /// <param name="item">获取到或新创建的缓存项。</param>
    /// <returns>如果成功获取或添加缓存项，则为 <c>true</c>；否则为 <c>false</c>。</returns>
    private bool TryGetOrAddInternal(string key, string region, Func<string, string, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
    {
        CacheItem<TValue> newItem = null;
        var tries = 0;
        do
        {
            tries++;
            item = GetCacheItemInternal(key, region);
            if (item != null)
            {
                return true;
            }

            // 重试时仅调用一次值工厂
            newItem ??= valueFactory(key, region);

            if (newItem == null)
            {
                return false;
            }

            if (AddInternal(newItem))
            {
                item = newItem;
                return true;
            }
        }
        while (tries <= Configuration.MaxRetries);

        return false;
    }

    /// <summary>
    /// 获取指定键的缓存项；若不存在，则通过值工厂创建并添加，并在重试耗尽后抛出异常。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
    /// <param name="valueFactory">用于创建缓存项的值工厂函数。</param>
    /// <returns>获取到或新创建的缓存项。</returns>
    /// <exception cref="InvalidOperationException">当值工厂返回 <c>null</c>，或在重试次数耗尽后仍无法获取或添加缓存项时抛出。</exception>
    private CacheItem<TValue> GetOrAddInternal(string key, string region, Func<string, string, CacheItem<TValue>> valueFactory)
    {
        CacheItem<TValue> newItem = null;
        var tries = 0;
        do
        {
            tries++;
            var item = GetCacheItemInternal(key, region);
            if (item != null)
            {
                return item;
            }

            // 重试时仅调用一次值工厂
            newItem ??= valueFactory(key, region);

            // 显式抛出异常以保持行为一致；否则稍后最终也会抛出。
            if (newItem == null)
            {
                throw new InvalidOperationException("The CacheItem which should be added must not be null.");
            }

            if (AddInternal(newItem))
            {
                return newItem;
            }
        }
        while (tries <= Configuration.MaxRetries);

        // 通常不应发生，但在极端情况下（例如最大重试次数为 1 且某项恰好在获取与添加之间被添加）可能出现。
        // 此情况非常罕见，因此将最大重试次数保持在 50 左右。
        throw new InvalidOperationException(
            string.Format("Could not get nor add the item {0} {1}", key, region));
    }
}
