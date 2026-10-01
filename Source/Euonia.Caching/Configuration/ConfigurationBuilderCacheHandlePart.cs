namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Used to build a <c>CacheHandleConfiguration</c>.
/// </summary>
/// <see cref="CacheManagerConfiguration"/>
public sealed class ConfigurationBuilderCacheHandlePart
{
    private readonly ConfigurationBuilderCachePart _parent;

    internal ConfigurationBuilderCacheHandlePart(CacheHandleConfiguration configuration, ConfigurationBuilderCachePart parentPart)
    {
        Configuration = configuration;
        _parent = parentPart;
    }

    /// <summary>
    /// Gets the parent builder part to add another cache configuration. Can be used to add
    /// multiple cache handles.
    /// </summary>
    /// <value>The parent builder part.</value>
    public ConfigurationBuilderCachePart And => _parent;

    internal CacheHandleConfiguration Configuration { get; }

    /// <summary>
    /// Hands back the new <see cref="CacheManagerConfiguration"/> instance.
    /// </summary>
    /// <returns>The <see cref="CacheManagerConfiguration"/>.</returns>
    public CacheManagerConfiguration Build()
    {
        return _parent.Build();
    }

    /// <summary>
    /// Disables statistic gathering for this cache handle.
    /// <para>This also disables performance counters as statistics are required for the counters.</para>
    /// </summary>
    /// <returns>The builder part.</returns>
    public ConfigurationBuilderCacheHandlePart DisableStatistics()
    {
        Configuration.EnableStatistics = false;
        return this;
    }

    /// <summary>
    /// Enables statistic gathering for this cache handle.
    /// <para>The statistics can be accessed via cacheHandle.Stats.GetStatistic.</para>
    /// </summary>
    /// <returns>The builder part.</returns>
    public ConfigurationBuilderCacheHandlePart EnableStatistics()
    {
        Configuration.EnableStatistics = true;
        return this;
    }

    /// <summary>
    /// Sets the expiration mode and timeout of the cache handle.
    /// </summary>
    /// <param name="expirationMode">The expiration mode.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>The builder part.</returns>
    /// <exception cref="InvalidOperationException">
    /// If expiration mode is not set to 'None', timeout cannot be zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if expiration mode is not 'None' and timeout is zero.
    /// </exception>
    /// <seealso cref="CacheExpirationMode"/>
    public ConfigurationBuilderCacheHandlePart WithExpiration(CacheExpirationMode expirationMode, TimeSpan timeout)
    {
        // fixed #192 (was missing check for "Default" mode)
        if (expirationMode != CacheExpirationMode.None && expirationMode != CacheExpirationMode.Default && timeout == TimeSpan.Zero)
        {
            throw new InvalidOperationException(Resources.IDS_TIMEOUT_CANNOT_BE_ZERO);
        }

        Configuration.ExpirationMode = expirationMode;
        Configuration.ExpirationTimeout = timeout;
        return this;
    }
}
