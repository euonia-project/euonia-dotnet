using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Used to build a <c>CacheManagerConfiguration</c>.
/// </summary>
/// <see cref="CacheManagerConfiguration"/>
public class ConfigurationBuilderCachePart
{
    internal ConfigurationBuilderCachePart()
    {
        Configuration = new CacheManagerConfiguration();
    }

    internal ConfigurationBuilderCachePart(CacheManagerConfiguration forConfiguration)
    {
        Check.EnsureNotNull(forConfiguration, nameof(forConfiguration));
        Configuration = forConfiguration;
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    /// <value>The configuration.</value>
    internal CacheManagerConfiguration Configuration { get; }

    /// <summary>
    /// Configures the backplane for the cache manager.
    /// <para>
    /// This is an optional feature. If specified, see the documentation for the
    /// <paramref name="backplaneType"/>. The <paramref name="configurationKey"/> might be used to
    /// reference another configuration item.
    /// </para>
    /// <para>
    /// If a backplane is defined, at least one cache handle must be marked as backplane
    /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
    /// </para>
    /// </summary>
    /// <param name="backplaneType">The type of the backplane implementation.</param>
    /// <param name="configurationKey">The name.</param>
    /// <param name="args">Additional arguments the type might need to get initialized.</param>
    /// <returns>The builder instance.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="configurationKey"/> is null.</exception>
    public ConfigurationBuilderCachePart WithBackplane(Type backplaneType, string configurationKey, params object[] args)
    {
        Check.EnsureNotNull(backplaneType, nameof(backplaneType));
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

        Configuration.BackplaneType = backplaneType;
        Configuration.BackplaneTypeArguments = args;
        Configuration.BackplaneConfigurationKey = configurationKey;
        return this;
    }

    /// <summary>
    /// Configures the backplane for the cache manager.
    /// <para>
    /// This is an optional feature. If specified, see the documentation for the
    /// <paramref name="backplaneType"/>. The <paramref name="configurationKey"/> might be used to
    /// reference another configuration item.
    /// </para>
    /// <para>
    /// If a backplane is defined, at least one cache handle must be marked as backplane
    /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
    /// </para>
    /// </summary>
    /// <param name="backplaneType">The type of the backplane implementation.</param>
    /// <param name="configurationKey">The configuration key.</param>
    /// <param name="channelName">The backplane channel name.</param>
    /// <param name="args">Additional arguments the type might need to get initialized.</param>
    /// <returns>The builder instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="configurationKey"/> or <paramref name="channelName"/> is null.
    /// </exception>
    public ConfigurationBuilderCachePart WithBackplane(Type backplaneType, string configurationKey, string channelName, params object[] args)
    {
        Check.EnsureNotNull(backplaneType, nameof(backplaneType));
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
        Check.EnsureNotNullOrWhiteSpace(channelName, nameof(channelName));

        Configuration.BackplaneType = backplaneType;
        Configuration.BackplaneTypeArguments = args;
        Configuration.BackplaneChannelName = channelName;
        Configuration.BackplaneConfigurationKey = configurationKey;
        return this;
    }

    /// <summary>
    /// Adds a cache dictionary cache handle to the cache manager.
    /// </summary>
    /// <param name="isBackplaneSource">
    /// Set this to true if this cache handle should be the source of the backplane.
    /// <para>This setting will be ignored if no backplane is configured.</para>
    /// </param>
    /// <returns>The builder part.</returns>
    public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(bool isBackplaneSource = false) =>
        WithHandle(typeof(DictionaryCacheHandle<>), Guid.NewGuid().ToString("N"), isBackplaneSource);

    /// <summary>
    /// Adds a cache dictionary cache handle to the cache manager.
    /// </summary>
    /// <returns>The builder part.</returns>
    /// <param name="handleName">The name of the cache handle.</param>
    /// <param name="isBackplaneSource">
    /// Set this to true if this cache handle should be the source of the backplane.
    /// <para>This setting will be ignored if no backplane is configured.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handleName"/> is null.</exception>
    public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(string handleName, bool isBackplaneSource = false) =>
        WithHandle(typeof(DictionaryCacheHandle<>), handleName, isBackplaneSource);

    /// <summary>
    /// Adds a cache handle with the given <c>Type</c> and name.
    /// The type must be an open generic.
    /// </summary>
    /// <param name="cacheHandleBaseType">The cache handle type.</param>
    /// <param name="handleName">The name to be used for the cache handle.</param>
    /// <param name="isBackplaneSource">
    /// Set this to true if this cache handle should be the source of the backplane.
    /// <para>This setting will be ignored if no backplane is configured.</para>
    /// </param>
    /// <param name="configurationTypes">Internally used only.</param>
    /// <returns>The builder part.</returns>
    /// <exception cref="ArgumentNullException">If handleName is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Only one cache handle can be the backplane's source.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handleName or cacheHandleBaseType are null.
    /// </exception>
    public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName, bool isBackplaneSource, params object[] configurationTypes)
    {
        Check.EnsureNotNull(cacheHandleBaseType, nameof(cacheHandleBaseType));
        Check.EnsureNotNullOrWhiteSpace(handleName, nameof(handleName));

        var handleCfg = new CacheHandleConfiguration(handleName)
        {
            HandleType = cacheHandleBaseType,
            ConfigurationTypes = configurationTypes,
            IsBackplaneSource = isBackplaneSource
        };

        if (isBackplaneSource && Configuration.CacheHandleConfigurations.Any(p => p.IsBackplaneSource))
        {
            throw new InvalidOperationException(Resources.IDS_ONLY_ONE_BACKPLANE_SOURCE);
        }

        Configuration.CacheHandleConfigurations.Add(handleCfg);
        var part = new ConfigurationBuilderCacheHandlePart(handleCfg, this);
        return part;
    }

    /// <summary>
    /// Adds a cache handle with the given <c>Type</c> and name.
    /// The type must be an open generic.
    /// </summary>
    /// <param name="cacheHandleBaseType">The cache handle type.</param>
    /// <param name="handleName">The name to be used for the cache handle.</param>
    /// <returns>The builder part.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handleName or cacheHandleBaseType are null.
    /// </exception>
    public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName)
        => WithHandle(cacheHandleBaseType, handleName, false);

    /// <summary>
    /// Adds a cache handle with the given <c>Type</c>.
    /// The type must be an open generic.
    /// </summary>
    /// <param name="cacheHandleBaseType">The cache handle type.</param>
    /// <returns>The builder part.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handleName or cacheHandleBaseType are null.
    /// </exception>
    public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType)
        => WithHandle(cacheHandleBaseType, Guid.NewGuid().ToString("N"), false);

    /// <summary>
    /// Sets the maximum number of retries per action.
    /// <para>Default is 50.</para>
    /// <para>
    /// Not every cache handle implements this, usually only distributed caches will use it.
    /// </para>
    /// </summary>
    /// <param name="retries">The maximum number of retries.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="InvalidOperationException">
    /// Maximum number of retries must be greater than 0.
    /// </exception>
    public ConfigurationBuilderCachePart WithMaxRetries(int retries)
    {
        Check.Ensure(retries > 0, "Maximum number of retries must be greater than 0.");

        Configuration.MaxRetries = retries;
        return this;
    }

    /// <summary>
    /// Sets the timeout between each retry of an action in milliseconds.
    /// <para>Default is 100.</para>
    /// <para>
    /// Not every cache handle implements this, usually only distributed caches will use it.
    /// </para>
    /// </summary>
    /// <param name="timeoutMillis">The timeout in milliseconds.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="InvalidOperationException">
    /// Retry timeout must be greater than or equal to zero.
    /// </exception>
    public ConfigurationBuilderCachePart WithRetryTimeout(int timeoutMillis)
    {
        Check.Ensure(timeoutMillis >= 0, "Retry timeout must be greater than or equal to zero.");

        Configuration.RetryTimeout = timeoutMillis;
        return this;
    }

    /// <summary>
    /// Sets the update mode of the cache.
    /// <para>If nothing is set, the default will be <c>CacheUpdateMode.None</c>.</para>
    /// </summary>
    /// <param name="updateMode">The update mode.</param>
    /// <returns>The builder part.</returns>
    /// <seealso cref="CacheUpdateMode"/>
    public ConfigurationBuilderCachePart WithUpdateMode(CacheUpdateMode updateMode)
    {
        Configuration.UpdateMode = updateMode;
        return this;
    }

    /// <summary>
    /// Hands back the new <see cref="CacheManagerConfiguration"/> instance.
    /// </summary>
    /// <returns>The <see cref="CacheManagerConfiguration"/>.</returns>
    public CacheManagerConfiguration Build()
    {
        return Configuration;
    }
}
