namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// <see cref="RedisConfigurationBuilder"/> 用于通过代码创建 <see cref="RedisConfiguration"/> 对象。
/// </summary>
public class RedisConfigurationBuilder
{
    private bool _allowAdmin;
    private int _connectionTimeout = 5000;
    private int _database;
    private readonly List<ServerEndPoint> _endpoints = new();
    private bool _isSsl;
    private readonly string _key;
    private string? _password;
    private string? _sslHost;
    private bool _enabledKeyspaceNotifications;
    private string? _useVersion;
    private bool _useTwemproxy;

    /// <summary>
    /// 初始化 <see cref="RedisConfigurationBuilder"/> 类的新实例。
    /// </summary>
    /// <param name="configurationKey">配置键。</param>
    /// <exception cref="ArgumentNullException">当 configurationKey 为 null 时抛出。</exception>
    public RedisConfigurationBuilder(string configurationKey)
    {
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

        _key = configurationKey;
    }

    /// <summary>
    /// 根据当前指定的属性创建 <see cref="RedisConfiguration"/>（如果可行）。
    /// </summary>
    /// <returns><c>RedisConfiguration</c>。</returns>
    public RedisConfiguration Build() =>
        new(
            key: _key,
            endpoints: _endpoints,
            database: _database,
            password: _password,
            isSsl: _isSsl,
            sslHost: _sslHost,
            connectionTimeout: _connectionTimeout,
            allowAdmin: _allowAdmin,
            keyspaceNotificationsEnabled: _enabledKeyspaceNotifications,
            twemproxyEnabled: _useTwemproxy,
            strictCompatibilityModeVersion: _useVersion);

    /// <summary>
    /// 启用该标志，使 CacheManager 响应来自 redis 的键空间通知。
    /// CacheManager 只会监听逐出和过期事件（并非所有事件）。
    /// 仅在你已正确配置 Redis 的情况下使用此功能：notify-keyspace-events 必须至少设置为 Exe。
    /// <see href="https://redis.io/topics/notifications#configuration"/>
    /// </summary>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder EnableKeyspaceEvents()
    {
        _enabledKeyspaceNotifications = true;
        return this;
    }

    /// <summary>
    /// 可用于控制 CacheManager 能够使用的 Redis 功能。例如，若设置为 <c>"2.4"</c>，将禁用所有 LUA 支持，
    /// 并强制 CacheManager 使用其他 API。
    /// </summary>
    /// <param name="version">要兼容的 Redis 版本号（例如 <c>"2.4"</c>）。</param>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder UseCompatibilityMode(string version)
    {
        _useVersion = version;
        return this;
    }

    /// <summary>
    /// 如果你在 Twemproxy 后面使用 Redis，请启用此项。
    /// </summary>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder UseTwemproxy()
    {
        _useTwemproxy = true;
        return this;
    }

    /// <summary>
    /// 若设置为 true，则启用可能具有风险的操作，例如会删除
    /// redis 数据库中所有条目的 Clear。
    /// </summary>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder WithAllowAdmin()
    {
        _allowAdmin = true;
        return this;
    }

    /// <summary>
    /// 设置连接操作的超时时间（毫秒）。
    /// </summary>
    /// <param name="timeout">超时时间（毫秒）。</param>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder WithConnectionTimeout(int timeout)
    {
        _connectionTimeout = timeout;
        return this;
    }

    /// <summary>
    /// 设置数据库。
    /// <para>数据库的最大数量取决于 redis 服务器配置。</para>默认为 <c>0</c>。
    /// </summary>
    /// <param name="databaseIndex">数据库索引。</param>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder WithDatabase(int databaseIndex)
    {
        _database = databaseIndex;
        return this;
    }

    /// <summary>
    /// 向连接配置添加一个终结点。
    /// <para>多次调用可添加多个终结点。</para>
    /// </summary>
    /// <param name="host">redis 服务器的主机或 IP。</param>
    /// <param name="port">redis 服务器的端口。</param>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder WithEndpoint(string host, int port)
    {
        var endpoint = new ServerEndPoint(host, port);
        _endpoints.Add(endpoint);
        return this;
    }

    /// <summary>
    /// 设置 redis 服务器的密码。
    /// </summary>
    /// <param name="serverPassword">redis 服务器密码。</param>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder WithPassword(string serverPassword)
    {
        _password = serverPassword;
        return this;
    }

    /// <summary>
    /// 启用 SSL 加密。
    /// <para>
    /// 若指定了主机，则会在服务器证书上强制校验特定的 SSL 主机标识。
    /// </para>
    /// </summary>
    /// <param name="host">SSL 主机。</param>
    /// <returns>构建器。</returns>
    public RedisConfigurationBuilder WithSsl(string? host = null)
    {
        _isSsl = true;
        _sslHost = host;
        return this;
    }
}