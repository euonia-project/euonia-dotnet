namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 基本的缓存管理器配置类。
/// </summary>
public sealed class CacheManagerConfiguration
{
    /// <summary>
    /// 初始化 <see cref="CacheManagerConfiguration"/> 类的新实例。
    /// </summary>
    public CacheManagerConfiguration()
    {
    }

    /// <summary>
    /// 获取当前 <see cref="CacheManagerConfiguration"/> 实例的 <see cref="ConfigurationBuilder"/>，
    /// 以便以流式方式修改配置。
    /// </summary>
    /// <returns><see cref="ConfigurationBuilder"/> 实例。</returns>
    public ConfigurationBuilder Builder => new(this);

    /// <summary>
    /// 获取或设置缓存的名称。
    /// </summary>
    /// <value>缓存的名称。</value>
    public string Name { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 获取或设置缓存管理器实例的 <see cref="UpdateMode"/>。
    /// <para>
    /// 控制缓存管理器应如何更新其管理的各个缓存句柄的行为。
    /// </para>
    /// </summary>
    /// <value>缓存更新模式。</value>
    /// <see cref="UpdateMode"/>
    public CacheUpdateMode UpdateMode { get; set; } = CacheUpdateMode.Up;

    /// <summary>
    /// 获取或设置每个操作的重试次数上限。
    /// <para>默认值为 50。</para>
    /// </summary>
    /// <value>最大重试次数。</value>
    public int MaxRetries { get; set; } = 50;

    /// <summary>
    /// 获取或设置缓存重试某个操作之前应等待的毫秒数。
    /// <para>默认值为 100。</para>
    /// </summary>
    /// <value>重试超时时间。</value>
    public int RetryTimeout { get; set; } = 100;

    /// <summary>
    /// 获取或设置背板可能使用的配置键。
    /// </summary>
    /// <value>背板配置的键。</value>
    public string BackplaneConfigurationKey { get; set; }

    /// <summary>
    /// 获取或设置背板通道名称。
    /// </summary>
    /// <value>通道名称。</value>
    public string BackplaneChannelName { get; set; }

    /// <summary>
    /// 获取一个值，指示此实例是否定义了背板。
    /// </summary>
    /// <value>
    /// 如果此实例有缓存背板，则为 <c>true</c>；否则为 <c>false</c>。
    /// </value>
    public bool HasBackplane => BackplaneType != null;

    /// <summary>
    /// 获取或设置缓存背板的工厂方法。
    /// </summary>
    /// <value>背板激活器。</value>
    public Type BackplaneType { get; set; }

    /// <summary>
    /// 获取或设置实例化背板时应使用的附加参数。
    /// </summary>
    /// <value>参数列表。</value>
    public object[] BackplaneTypeArguments { get; set; }

    /// <summary>
    /// 获取缓存句柄配置的列表。
    /// </summary>
    /// <value>缓存句柄配置的列表。</value>
    public IList<CacheHandleConfiguration> CacheHandleConfigurations { get; } = new List<CacheHandleConfiguration>();

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name}: {string.Join(", ", CacheHandleConfigurations)}";
    }
}