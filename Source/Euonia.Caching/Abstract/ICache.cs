namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 此接口是本库主要体系的基础约定。
/// <para>
/// <c>ICacheHandle</c> 和 <c>ICacheManager</c> 接口派生自 <c>ICache</c>，
/// 这意味着整个体系中的方法调用签名非常相似。
/// </para>
/// <para>
/// 我们希望具备足够的灵活性，既能支持简单的 get/put/delete 缓存，也能支持多层叠加的多个缓存，
/// 同时仍然使用同样简单易懂的接口。
/// </para>
/// <para>
/// 可以在强类型意义上使用 <typeparamref name="TValue"/>，但大多数情况下没有必要。这意味着你可以在自己的领域中
/// 为特定对象类型定义并配置缓存。但你也可以使用 <c>object</c> 并在缓存中存储任何内容。所有底层
/// 缓存技术通常并不关心缓存项的类型。
/// </para>
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
public interface ICache<TValue> : IDisposable
{
    /// <summary>
    /// 获取或设置指定键的值。索引器等效于对应的 <see cref="Put(string, TValue)"/> 和 <see cref="Get(string)"/> 调用。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <returns>缓存中为给定 <paramref name="key"/> 存储的值。</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="key"/> 为 null。</exception>
    TValue this[string key] { get; set; }

    /// <summary>
    /// 获取或设置指定键和区域的值。索引器等效于对应的 <see cref="Put(string, TValue, string)"/> 和
    /// <see cref="Get(string, string)"/> 调用。
    /// <para>
    /// 指定 <paramref name="region"/> 后，将<b>不会</b>在全局缓存中找到该键。
    /// </para>
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>
    /// 缓存中为给定 <paramref name="key"/> 和 <paramref name="region"/> 存储的值。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    TValue this[string key, string region] { get; set; }

    /// <summary>
    /// 将指定键的值添加到缓存中。
    /// <para>
    /// 如果指定的 <paramref name="key"/> 已存在于缓存中，<c>Add</c> 方法将<b>不会</b>成功！
    /// </para>
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="value">应被缓存的值。</param>
    /// <returns>
    /// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="value"/> 为 null。
    /// </exception>
    bool Add(string key, TValue value);

    /// <summary>
    /// 将指定键和区域的值添加到缓存中。
    /// <para>
    /// 如果指定的 <paramref name="key"/> 已存在于缓存中，<c>Add</c> 方法将<b>不会</b>成功！
    /// </para>
    /// <para>
    /// 指定 <paramref name="region"/> 后，将<b>不会</b>在全局缓存中找到该键。
    /// </para>
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="value">应被缓存的值。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>
    /// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="value"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    bool Add(string key, TValue value, string region);

    /// <summary>
    /// 将指定的 <c>CacheItem</c> 添加到缓存中。
    /// <para>
    /// 使用此重载可以覆盖缓存已配置的过期设置，仅为该 <paramref name="item"/> 定义自定义过期时间。
    /// </para>
    /// <para>
    /// 如果指定的 <paramref name="item"/> 已存在于缓存中，<c>Add</c> 方法将<b>不会</b>成功！
    /// </para>
    /// </summary>
    /// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
    /// <returns>
    /// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="item"/> 或该项的键或值为 null。
    /// </exception>
    bool Add(CacheItem<TValue> item);

    /// <summary>
    /// 清空此缓存，移除基础缓存和所有区域中的所有项。
    /// </summary>
    void Clear();

    /// <summary>
    /// 清空缓存区域，仅移除指定 <paramref name="region"/> 中的所有项。
    /// </summary>
    /// <param name="region">缓存区域。</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="region"/> 为 null。</exception>
    void ClearRegion(string region);

    /// <summary>
    /// 返回一个值，指示 <paramref name="key"/> 是否存在于 CacheManager 中配置的至少一个缓存层中，
    /// 而不实际从缓存中检索它。
    /// </summary>
    /// <param name="key">要检查的缓存键。</param>
    /// <returns>如果 <paramref name="key"/> 存在，则为 <c>True</c>；否则为 <c>False</c>。</returns>
    bool Exists(string key);

    /// <summary>
    /// 返回一个值，指示 <paramref name="region"/> 中的 <paramref name="key"/> 是否存在于 CacheManager 中配置的至少一个缓存层中，
    /// 而不实际从缓存中检索它（如果支持）。
    /// </summary>
    /// <param name="key">要检查的缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>如果 <paramref name="key"/> 存在，则为 <c>True</c>；否则为 <c>False</c>。</returns>
    bool Exists(string key, string region);

    /// <summary>
    /// 获取指定键的值。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <returns>缓存中为给定 <paramref name="key"/> 存储的值。</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="key"/> 为 null。</exception>
    TValue Get(string key);

    /// <summary>
    /// 获取指定键和区域的值。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>
    /// 缓存中为给定 <paramref name="key"/> 和 <paramref name="region"/> 存储的值。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    TValue Get(string key, string region);

    /// <summary>
    /// 获取指定键的值，并将其转换为指定类型。
    /// </summary>
    /// <typeparam name="TOut">值转换后返回的类型。</typeparam>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <returns>缓存中为给定 <paramref name="key"/> 存储的值。</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="key"/> 为 null。</exception>
    /// <exception cref="InvalidCastException">
    /// 如果无法将 <typeparamref name="TValue"/> 转换为 <typeparamref name="TOut"/>。
    /// </exception>
    TOut Get<TOut>(string key);

    /// <summary>
    /// 获取指定键和区域的值，并将其转换为指定类型。
    /// </summary>
    /// <typeparam name="TOut">缓存值应转换成的类型。</typeparam>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>
    /// 缓存中为给定 <paramref name="key"/> 和 <paramref name="region"/> 存储的值。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// 如果无法将 <typeparamref name="TValue"/> 转换为 <typeparamref name="TOut"/>。
    /// </exception>
    TOut Get<TOut>(string key, string region);

    /// <summary>
    /// 获取指定键的 <c>CacheItem</c>。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <returns>缓存中为给定 <paramref name="key"/> 存储的 <c>CacheItem</c>；如果该项不存在或已过期，则为 <c>null</c>。</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="key"/> 为 null。</exception>
    CacheItem<TValue>? GetCacheItem(string key);

    /// <summary>
    /// 获取指定键和区域的 <c>CacheItem</c>。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>
    /// 缓存中为给定 <paramref name="key"/> 和 <paramref name="region"/> 存储的 <c>CacheItem</c>；
    /// 如果该项不存在或已过期，则为 <c>null</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    CacheItem<TValue>? GetCacheItem(string key, string region);

    /// <summary>
    /// 将指定键的值放入缓存中。
    /// <para>
    /// 如果 <paramref name="key"/> 已存在于缓存中，则现有值将被新的 <paramref name="value"/> 替换。
    /// </para>
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="value">应被缓存的值。</param>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="value"/> 为 null。
    /// </exception>
    void Put(string key, TValue value);

    /// <summary>
    /// 将指定键和区域的值放入缓存中。
    /// <para>
    /// 如果 <paramref name="key"/> 已存在于缓存中，则现有值将被新的 <paramref name="value"/> 替换。
    /// </para>
    /// <para>
    /// 指定 <paramref name="region"/> 后，将<b>不会</b>在全局缓存中找到该键。
    /// </para>
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="value">应被缓存的值。</param>
    /// <param name="region">缓存区域。</param>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="value"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    void Put(string key, TValue value, string region);

    /// <summary>
    /// 将指定的 <c>CacheItem</c> 放入缓存中。
    /// <para>
    /// 如果 <paramref name="item"/> 已存在于缓存中，则现有项将被新的 <paramref name="item"/> 替换。
    /// </para>
    /// <para>
    /// 使用此重载可以覆盖缓存已配置的过期设置，仅为该 <paramref name="item"/> 定义自定义过期时间。
    /// </para>
    /// </summary>
    /// <param name="item">要缓存的 <c>CacheItem</c>。</param>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="item"/> 或该项的键或值为 null。
    /// </exception>
    void Put(CacheItem<TValue> item);

    /// <summary>
    /// 从缓存中移除指定键的值。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <returns>
    /// 如果找到该键并将其从缓存中移除，则为 <c>true</c>；否则为 <c>false</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="key"/> 为 null。</exception>
    bool Remove(string key);

    /// <summary>
    /// 从缓存中移除指定键和区域的值。
    /// </summary>
    /// <param name="key">用于标识缓存中项的键。</param>
    /// <param name="region">缓存区域。</param>
    /// <returns>
    /// 如果找到该键并将其从缓存中移除，则为 <c>true</c>；否则为 <c>false</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="region"/> 为 null。
    /// </exception>
    bool Remove(string key, string region);

    /// <summary>
    /// 按前缀移除缓存项；默认实现不支持时返回 0。
    /// </summary>
    /// <param name="prefix">键前缀；应按原样前缀匹配。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>移除的条目数。</returns>
    /// <remarks>
    /// 带租户前缀的缓存键应传入 <c>t:{tenantId}:</c>。内存与 Redis 实现提供实际删除；其他实现默认不支持。
    /// </remarks>
    Task<long> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }
}