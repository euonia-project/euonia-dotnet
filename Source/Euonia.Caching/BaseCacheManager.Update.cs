using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching;

public partial class BaseCacheManager<TValue>
{
    /// <inheritdoc />
    public TValue AddOrUpdate(string key, TValue addValue, Func<TValue, TValue> updateValue) =>
        AddOrUpdate(key, addValue, updateValue, Configuration.MaxRetries);

    /// <inheritdoc />
    public TValue AddOrUpdate(string key, string region, TValue addValue, Func<TValue, TValue> updateValue) =>
        AddOrUpdate(key, region, addValue, updateValue, Configuration.MaxRetries);

    /// <inheritdoc />
    public TValue AddOrUpdate(string key, TValue addValue, Func<TValue, TValue> updateValue, int maxRetries) =>
        AddOrUpdate(new CacheItem<TValue>(key, addValue), updateValue, maxRetries);

    /// <inheritdoc />
    public TValue AddOrUpdate(string key, string region, TValue addValue, Func<TValue, TValue> updateValue, int maxRetries) =>
        AddOrUpdate(new CacheItem<TValue>(key, region, addValue), updateValue, maxRetries);

    /// <inheritdoc />
    public TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue) =>
        AddOrUpdate(addItem, updateValue, Configuration.MaxRetries);

    /// <inheritdoc />
    public TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue, int maxRetries)
    {
        Check.EnsureNotNull(addItem, nameof(addItem));
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        Check.Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

        return AddOrUpdateInternal(addItem, updateValue, maxRetries);
    }

    /// <summary>
    /// 执行添加或更新操作：先尝试添加，若失败（项已存在）则尝试更新，直到成功或重试次数耗尽。
    /// </summary>
    /// <param name="item">要添加的缓存项。</param>
    /// <param name="updateValue">用于更新现有值的函数。</param>
    /// <param name="maxRetries">最大重试次数。</param>
    /// <returns>添加或更新后的缓存值；重试耗尽时返回默认值。</returns>
    private TValue AddOrUpdateInternal(CacheItem<TValue> item, Func<TValue, TValue> updateValue, int maxRetries)
    {
        CheckDisposed();

        var tries = 0;
        do
        {
            tries++;

            if (AddInternal(item))
            {
                return item.Value;
            }

            TValue returnValue;
            var updated = string.IsNullOrWhiteSpace(item.Region) ? TryUpdate(item.Key, updateValue, maxRetries, out returnValue) : TryUpdate(item.Key, item.Region, updateValue, maxRetries, out returnValue);

            if (updated)
            {
                return returnValue;
            }
        }
        while (tries <= maxRetries);

        // 重试次数已耗尽，操作失败...（在 99.99% 的情况下不应发生，但也许应抛出异常？）
        return default;
    }

    /// <inheritdoc />
    public bool TryUpdate(string key, Func<TValue, TValue> updateValue, out TValue value) =>
        TryUpdate(key, updateValue, Configuration.MaxRetries, out value);

    /// <inheritdoc />
    public bool TryUpdate(string key, string region, Func<TValue, TValue> updateValue, out TValue value) =>
        TryUpdate(key, region, updateValue, Configuration.MaxRetries, out value);

    /// <inheritdoc />
    public bool TryUpdate(string key, Func<TValue, TValue> updateValue, int maxRetries, out TValue value)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        Check.Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

        return UpdateInternal(_cacheHandles, key, updateValue, maxRetries, false, out value);
    }

    /// <inheritdoc />
    public bool TryUpdate(string key, string region, Func<TValue, TValue> updateValue, int maxRetries, out TValue value)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        Check.Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

        return UpdateInternal(_cacheHandles, key, region, updateValue, maxRetries, false, out value);
    }

    /// <inheritdoc />
    public TValue Update(string key, Func<TValue, TValue> updateValue) =>
        Update(key, updateValue, Configuration.MaxRetries);

    /// <inheritdoc />
    public TValue Update(string key, string region, Func<TValue, TValue> updateValue) =>
        Update(key, region, updateValue, Configuration.MaxRetries);

    /// <inheritdoc />
    public TValue Update(string key, Func<TValue, TValue> updateValue, int maxRetries)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        Check.Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

        UpdateInternal(_cacheHandles, key, updateValue, maxRetries, true, out var value);

        return value;
    }

    /// <inheritdoc />
    public TValue Update(string key, string region, Func<TValue, TValue> updateValue, int maxRetries)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
        Check.EnsureNotNull(updateValue, nameof(updateValue));
        Check.Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

        UpdateInternal(_cacheHandles, key, region, updateValue, maxRetries, true, out var value);

        return value;
    }

    /// <summary>
    /// 在最低层缓存句柄上执行更新操作，并同步其他句柄与背板（不带区域的重载，转发到带区域版本）。
    /// </summary>
    /// <param name="handles">缓存句柄数组。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="updateValue">用于更新现有值的函数。</param>
    /// <param name="maxRetries">最大重试次数。</param>
    /// <param name="throwOnFailure">失败时是否抛出异常。</param>
    /// <param name="value">更新后的缓存值。</param>
    /// <returns>如果更新成功，则为 <c>true</c>；否则为 <c>false</c>。</returns>
    private bool UpdateInternal(BaseCacheHandle<TValue>[] handles,
                                string key,
                                Func<TValue, TValue> updateValue,
                                int maxRetries,
                                bool throwOnFailure,
                                out TValue value) =>
        UpdateInternal(handles, key, null, updateValue, maxRetries, throwOnFailure, out value);

    /// <summary>
    /// 在最低层缓存句柄上执行更新操作，并根据结果逐出或同步其他句柄，最后更新背板。
    /// </summary>
    /// <param name="handles">缓存句柄数组。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
    /// <param name="updateValue">用于更新现有值的函数。</param>
    /// <param name="maxRetries">最大重试次数。</param>
    /// <param name="throwOnFailure">失败时是否抛出异常。</param>
    /// <param name="value">更新后的缓存值。</param>
    /// <returns>如果更新成功，则为 <c>true</c>；否则为 <c>false</c>。</returns>
    private bool UpdateInternal(BaseCacheHandle<TValue>[] handles,
                                string key,
                                string region,
                                Func<TValue, TValue> updateValue,
                                int maxRetries,
                                bool throwOnFailure,
                                out TValue value)
    {
        CheckDisposed();

        // 赋默认值
        value = default;

        if (handles.Length == 0)
        {
            return false;
        }

        // 最低层级句柄
        // todo: 或许应检查仅在配置了背板时才在其上运行（该句柄可能并非最后一个）。
        var handleIndex = handles.Length - 1;
        var handle = handles[handleIndex];

        var result = string.IsNullOrWhiteSpace(region) ? handle.Update(key, updateValue, maxRetries) : handle.Update(key, region, updateValue, maxRetries);

        switch (result.UpdateState)
        {
            case CacheItemUpdateResultState.Success:
                // 仅在成功时，返回值不会为 null
                value = result.Value.Value;
                handle.Stats.OnUpdate(key, region, result);

                // 逐出其他句柄中的该项，因为我们不知道其他句柄上的更新是否真的能成功……
                // 存在一种风险：其他句柄上的更新可能产生与第一次成功更新不同的版本……
                // 不过我们可以安全地将该项添加到我们下方的句柄中。
                EvictFromHandlesAbove(key, region, handleIndex);

                // 优化：无需再次从缓存中获取该项，我们已经拥有它
                // var item = string.IsNullOrWhiteSpace(region) ? handle.GetCacheItem(key) : handle.GetCacheItem(key, region);
                AddToHandlesBelow(result.Value, handleIndex);
                TriggerOnUpdate(key, region);
                break;
            case CacheItemUpdateResultState.FactoryReturnedNull when throwOnFailure:
                throw new InvalidOperationException($"Update failed on '{region}:{key}' because value factory returned null.");
            case CacheItemUpdateResultState.TooManyRetries:
            {
                // 如果重试次数过多，这基本上表明缓存处于无效状态：
                // 该项确实存在，但我们无法更新它，而且它很可能具有不同的版本。
                EvictFromOtherHandles(key, region, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because of too many retries: {result.NumberOfTriesNeeded}.");
                }

                break;
            }
            case CacheItemUpdateResultState.ItemDidNotExist:
            {
                // 如果更新因项不存在而失败，且当前句柄是背板源或最低缓存句柄层级，
                // 则从其他句柄中移除该项（如果存在）。
                // 否则，如果我们在此处不退出，对下一个句柄调用更新可能会成功并返回误导性的结果。
                EvictFromOtherHandles(key, region, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because the region/key did not exist.");
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        // 更新背板
        if (result.UpdateState == CacheItemUpdateResultState.Success && _cacheBackplane != null)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                _cacheBackplane.NotifyChange(key, CacheItemChangedEventAction.Update);
            }
            else
            {
                _cacheBackplane.NotifyChange(key, region, CacheItemChangedEventAction.Update);
            }
        }

        {
        }

        return result.UpdateState == CacheItemUpdateResultState.Success;
    }
}