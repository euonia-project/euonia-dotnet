namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 定义缓存句柄应遵守的所有设置。
/// </summary>
public sealed class CacheHandleConfiguration
{
    /// <summary>
    /// 初始化 <see cref="CacheHandleConfiguration"/> 类的新实例。
    /// </summary>
    public CacheHandleConfiguration()
    {
        Name = Key = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 初始化 <see cref="CacheHandleConfiguration"/> 类的新实例。
    /// </summary>
    /// <param name="handleName">句柄的名称。此值也将用作 <see cref="Key"/>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="handleName"/> 为 <c>null</c> 时抛出。</exception>
    public CacheHandleConfiguration(string handleName)
    {
        Check.EnsureNotNullOrWhiteSpace(handleName, nameof(handleName));

        Name = Key = handleName;
    }

    /// <summary>
    /// 初始化 <see cref="CacheHandleConfiguration"/> 类的新实例。
    /// </summary>
    /// <param name="handleName">句柄的名称。</param>
    /// <param name="configurationKey">可用于标识句柄可能需要的配置另一部分的键。</param>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="handleName"/> 或 <paramref name="configurationKey"/> 为 <c>null</c> 时抛出。
    /// </exception>
    public CacheHandleConfiguration(string handleName, string configurationKey)
    {
        Check.EnsureNotNullOrWhiteSpace(handleName, nameof(handleName));
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

        Name = handleName;
        Key = configurationKey;
    }

    /// <summary>
    /// 获取或设置一个值，指示是否启用统计信息。
    /// </summary>
    /// <value>如果应启用统计信息，则为 <c>true</c>；否则为 <c>false</c>。</value>
    public bool EnableStatistics { get; set; }

    /// <summary>
    /// 获取或设置过期模式。
    /// </summary>
    /// <value>过期模式。</value>
    public CacheExpirationMode ExpirationMode { get; set; }

    /// <summary>
    /// 获取或设置过期时间。
    /// </summary>
    /// <value>过期时间。</value>
    public TimeSpan ExpirationTimeout { get; set; }

    /// <summary>
    /// 获取或设置缓存句柄的名称，该名称同时也是配置的标识符。
    /// </summary>
    /// <value>句柄的名称。</value>
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置配置键。
    /// 某些缓存句柄需要通过名称引用配置的另一部分。
    /// 如果未指定，则将使用 <see cref="Name"/>。
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示此实例是否为背板源。
    /// <para>
    /// 一个缓存管理器中只能有一个缓存句柄作为背板源。通常这应是
    /// 分布式缓存。将进程内缓存定义为背板源可能没有意义。
    /// </para>
    /// <para>如果未为缓存配置背板，则此设置将不起作用。</para>
    /// </summary>
    /// <value>如果此实例应为背板源，则为 <c>true</c>；否则为 <c>false</c>。</value>
    public bool IsBackplaneSource { get; set; }

    /// <summary>
    /// 获取或设置句柄的类型。
    /// </summary>
    /// <value>句柄的类型。</value>
    public Type HandleType { get; set; }

    internal object[] ConfigurationTypes { get; set; } = [];

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{HandleType}";
    }
}