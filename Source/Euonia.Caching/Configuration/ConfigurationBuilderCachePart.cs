using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 用于构建 <c>CacheManagerConfiguration</c>。
/// </summary>
/// <see cref="CacheManagerConfiguration"/>
public class ConfigurationBuilderCachePart
{
    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilderCachePart"/> 的新实例，并创建空的缓存管理器配置。
    /// </summary>
    internal ConfigurationBuilderCachePart()
    {
        Configuration = new CacheManagerConfiguration();
    }

    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilderCachePart"/> 的新实例，在已有配置上继续构建。
    /// </summary>
    /// <param name="forConfiguration">要继续构建的缓存管理器配置。</param>
    internal ConfigurationBuilderCachePart(CacheManagerConfiguration forConfiguration)
    {
        Check.EnsureNotNull(forConfiguration, nameof(forConfiguration));
        Configuration = forConfiguration;
    }

    /// <summary>
    /// 获取配置。
    /// </summary>
    /// <value>配置。</value>
    internal CacheManagerConfiguration Configuration { get; }

    /// <summary>
    /// 为缓存管理器配置背板。
    /// <para>
    /// 这是可选功能。若指定，请参阅 <paramref name="backplaneType"/> 的文档。
    /// <paramref name="configurationKey"/> 可用于引用另一个配置项。
    /// </para>
    /// <para>
    /// 如果定义了背板，则必须至少将一个缓存句柄标记为背板来源。
    /// 随后缓存管理器将尝试同步同一配置的多个实例。
    /// </para>
    /// </summary>
    /// <param name="backplaneType">背板实现的类型。</param>
    /// <param name="configurationKey">名称。</param>
    /// <param name="args">该类型初始化时可能需要的额外参数。</param>
    /// <returns>构建器实例。</returns>
    /// <exception cref="ArgumentNullException">如果 <paramref name="configurationKey"/> 为 null。</exception>
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
    /// 为缓存管理器配置背板。
    /// <para>
    /// 这是可选功能。若指定，请参阅 <paramref name="backplaneType"/> 的文档。
    /// <paramref name="configurationKey"/> 可用于引用另一个配置项。
    /// </para>
    /// <para>
    /// 如果定义了背板，则必须至少将一个缓存句柄标记为背板来源。
    /// 随后缓存管理器将尝试同步同一配置的多个实例。
    /// </para>
    /// </summary>
    /// <param name="backplaneType">背板实现的类型。</param>
    /// <param name="configurationKey">配置键。</param>
    /// <param name="channelName">背板通道名称。</param>
    /// <param name="args">该类型初始化时可能需要的额外参数。</param>
    /// <returns>构建器实例。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="configurationKey"/> 或 <paramref name="channelName"/> 为 null。
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
    /// 向缓存管理器添加一个基于字典的缓存句柄。
    /// </summary>
    /// <param name="isBackplaneSource">
    /// 如果此缓存句柄应作为背板来源，请设为 true。
    /// <para>如果未配置背板，则忽略此设置。</para>
    /// </param>
    /// <returns>构建器部件。</returns>
    public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(bool isBackplaneSource = false) =>
        WithHandle(typeof(DictionaryCacheHandle<>), Guid.NewGuid().ToString("N"), isBackplaneSource);

    /// <summary>
    /// 向缓存管理器添加一个基于字典的缓存句柄。
    /// </summary>
    /// <returns>构建器部件。</returns>
    /// <param name="handleName">缓存句柄的名称。</param>
    /// <param name="isBackplaneSource">
    /// 如果此缓存句柄应作为背板来源，请设为 true。
    /// <para>如果未配置背板，则忽略此设置。</para>
    /// </param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="handleName"/> 为 null，则抛出此异常。</exception>
    public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(string handleName, bool isBackplaneSource = false) =>
        WithHandle(typeof(DictionaryCacheHandle<>), handleName, isBackplaneSource);

    /// <summary>
    /// 添加具有指定 <c>Type</c> 和名称的缓存句柄。
    /// 该类型必须是开放泛型。
    /// </summary>
    /// <param name="cacheHandleBaseType">缓存句柄类型。</param>
    /// <param name="handleName">用于该缓存句柄的名称。</param>
    /// <param name="isBackplaneSource">
    /// 如果此缓存句柄应作为背板来源，请设为 true。
    /// <para>如果未配置背板，则忽略此设置。</para>
    /// </param>
    /// <param name="configurationTypes">仅供内部使用。</param>
    /// <returns>构建器部件。</returns>
    /// <exception cref="ArgumentNullException">如果 handleName 为 null。</exception>
    /// <exception cref="InvalidOperationException">
    /// 只能有一个缓存句柄作为背板来源。
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// 如果 handleName 或 cacheHandleBaseType 为 null，则抛出此异常。
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
    /// 添加具有指定 <c>Type</c> 和名称的缓存句柄。
    /// 该类型必须是开放泛型。
    /// </summary>
    /// <param name="cacheHandleBaseType">缓存句柄类型。</param>
    /// <param name="handleName">用于该缓存句柄的名称。</param>
    /// <returns>构建器部件。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 handleName 或 cacheHandleBaseType 为 null，则抛出此异常。
    /// </exception>
    public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName)
        => WithHandle(cacheHandleBaseType, handleName, false);

    /// <summary>
    /// 添加具有指定 <c>Type</c> 的缓存句柄。
    /// 该类型必须是开放泛型。
    /// </summary>
    /// <param name="cacheHandleBaseType">缓存句柄类型。</param>
    /// <returns>构建器部件。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 handleName 或 cacheHandleBaseType 为 null，则抛出此异常。
    /// </exception>
    public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType)
        => WithHandle(cacheHandleBaseType, Guid.NewGuid().ToString("N"), false);

    /// <summary>
    /// 设置每个操作的最大重试次数。
    /// <para>默认为 50。</para>
    /// <para>
    /// 并非所有缓存句柄都实现此项，通常只有分布式缓存会用到。
    /// </para>
    /// </summary>
    /// <param name="retries">最大重试次数。</param>
    /// <returns>配置构建器。</returns>
    /// <exception cref="InvalidOperationException">
    /// 最大重试次数必须大于 0。
    /// </exception>
    public ConfigurationBuilderCachePart WithMaxRetries(int retries)
    {
        Check.Ensure(retries > 0, "Maximum number of retries must be greater than 0.");

        Configuration.MaxRetries = retries;
        return this;
    }

    /// <summary>
    /// 设置每次操作重试之间的超时时间（毫秒）。
    /// <para>默认为 100。</para>
    /// <para>
    /// 并非所有缓存句柄都实现此项，通常只有分布式缓存会用到。
    /// </para>
    /// </summary>
    /// <param name="timeoutMillis">超时时间（毫秒）。</param>
    /// <returns>配置构建器。</returns>
    /// <exception cref="InvalidOperationException">
    /// 重试超时时间必须大于或等于零。
    /// </exception>
    public ConfigurationBuilderCachePart WithRetryTimeout(int timeoutMillis)
    {
        Check.Ensure(timeoutMillis >= 0, "Retry timeout must be greater than or equal to zero.");

        Configuration.RetryTimeout = timeoutMillis;
        return this;
    }

    /// <summary>
    /// 设置缓存的更新模式。
    /// <para>如果未设置任何值，默认使用 <c>CacheUpdateMode.None</c>。</para>
    /// </summary>
    /// <param name="updateMode">更新模式。</param>
    /// <returns>构建器部件。</returns>
    /// <seealso cref="CacheUpdateMode"/>
    public ConfigurationBuilderCachePart WithUpdateMode(CacheUpdateMode updateMode)
    {
        Configuration.UpdateMode = updateMode;
        return this;
    }

    /// <summary>
    /// 返回新的 <see cref="CacheManagerConfiguration"/> 实例。
    /// </summary>
    /// <returns>该 <see cref="CacheManagerConfiguration"/>。</returns>
    public CacheManagerConfiguration Build()
    {
        return Configuration;
    }
}
