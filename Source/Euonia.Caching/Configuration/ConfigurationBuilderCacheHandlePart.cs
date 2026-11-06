namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 用于构建 <c>CacheHandleConfiguration</c>。
/// </summary>
/// <see cref="CacheManagerConfiguration"/>
public sealed class ConfigurationBuilderCacheHandlePart
{
    private readonly ConfigurationBuilderCachePart _parent;

    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilderCacheHandlePart"/> 的新实例。
    /// </summary>
    /// <param name="configuration">要构建的缓存句柄配置。</param>
    /// <param name="parentPart">父构建器部件，用于继续添加句柄或构建配置。</param>
    internal ConfigurationBuilderCacheHandlePart(CacheHandleConfiguration configuration, ConfigurationBuilderCachePart parentPart)
    {
        Configuration = configuration;
        _parent = parentPart;
    }

    /// <summary>
    /// 获取父构建器部件，以便添加另一个缓存配置。可用于添加多个缓存句柄。
    /// </summary>
    /// <value>父构建器部件。</value>
    public ConfigurationBuilderCachePart And => _parent;

    /// <summary>
    /// 获取当前正在构建的缓存句柄配置。
    /// </summary>
    /// <value>句柄配置实例。</value>
    internal CacheHandleConfiguration Configuration { get; }

    /// <summary>
    /// 返回新的 <see cref="CacheManagerConfiguration"/> 实例。
    /// </summary>
    /// <returns>该 <see cref="CacheManagerConfiguration"/>。</returns>
    public CacheManagerConfiguration Build()
    {
        return _parent.Build();
    }

    /// <summary>
    /// 禁用此缓存句柄的统计信息收集。
    /// <para>这同时也会禁用性能计数器，因为计数器需要依赖统计数据。</para>
    /// </summary>
    /// <returns>构建器部件。</returns>
    public ConfigurationBuilderCacheHandlePart DisableStatistics()
    {
        Configuration.EnableStatistics = false;
        return this;
    }

    /// <summary>
    /// 启用此缓存句柄的统计信息收集。
    /// <para>可通过 cacheHandle.Stats.GetStatistic 访问统计数据。</para>
    /// </summary>
    /// <returns>构建器部件。</returns>
    public ConfigurationBuilderCacheHandlePart EnableStatistics()
    {
        Configuration.EnableStatistics = true;
        return this;
    }

    /// <summary>
    /// 设置缓存句柄的过期模式和超时时间。
    /// </summary>
    /// <param name="expirationMode">过期模式。</param>
    /// <param name="timeout">超时时间。</param>
    /// <returns>构建器部件。</returns>
    /// <exception cref="InvalidOperationException">
    /// 如果过期模式未设置为 'None'，则超时时间不能为零。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 如果过期模式不是 'None' 且超时时间为零，则抛出此异常。
    /// </exception>
    /// <seealso cref="CacheExpirationMode"/>
    public ConfigurationBuilderCacheHandlePart WithExpiration(CacheExpirationMode expirationMode, TimeSpan timeout)
    {
        // 修复 #192（原先遗漏了对 "Default" 模式的检查）
        if (expirationMode != CacheExpirationMode.None && expirationMode != CacheExpirationMode.Default && timeout == TimeSpan.Zero)
        {
            throw new InvalidOperationException(Resources.IDS_TIMEOUT_CANNOT_BE_ZERO);
        }

        Configuration.ExpirationMode = expirationMode;
        Configuration.ExpirationTimeout = timeout;
        return this;
    }
}
