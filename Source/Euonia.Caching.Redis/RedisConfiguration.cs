using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// <see cref="RedisConfiguration"/> 用于通过代码或配置文件配置例如 StackExchange.Redis。
/// <para>
/// 添加该元素只是因为 StackExchange.Redis 不支持通过 web/app.config 进行配置
/// </para>
/// </summary>
public class RedisConfiguration
{
    private string _connectionString = null!;
    private ConfigurationOptions _configurationOptions = null!;

    /// <summary>
    /// 初始化 <see cref="RedisConfiguration"/> 类的新实例。
    /// </summary>
    public RedisConfiguration()
    {
    }

    /// <summary>
    /// 初始化 <see cref="RedisConfiguration"/> 类的新实例。
    /// </summary>
    /// <param name="key">
    /// 配置键，缓存句柄将用它来查找与缓存句柄名称对应的配置。
    /// </param>
    /// <param name="endpoints">
    /// 用于连接 Redis 服务器的 <see cref="ServerEndPoint"/> 列表。
    /// </param>
    /// <param name="database">Redis 数据库索引。</param>
    /// <param name="password">Redis 服务器的密码。</param>
    /// <param name="isSsl">如果为 <c>true</c>，则指示缓存使用 SSL 加密。</param>
    /// <param name="sslHost">如果指定，连接将设置 SSL 主机。</param>
    /// <param name="connectionTimeout">设置用于连接操作的超时时间。</param>
    /// <param name="allowAdmin">
    /// 如果设置为 <c>True</c>，则允许缓存使用可能有风险的功能，
    /// 例如 <c>Clear</c>。
    /// </param>
    /// <param name="keyspaceNotificationsEnabled">启用键空间通知，以便对项的逐出/过期做出反应。</param>
    /// <param name="twemproxyEnabled">启用 Twemproxy 模式。</param>
    /// <param name="strictCompatibilityModeVersion">
    /// 获取或设置版本号，用于限制缓存管理器可用的功能。
    /// </param>
    public RedisConfiguration(string key,
                              IList<ServerEndPoint> endpoints,
                              int database = 0,
                              string? password = null,
                              bool isSsl = false,
                              string? sslHost = null,
                              int connectionTimeout = 5000,
                              bool allowAdmin = false,
                              bool keyspaceNotificationsEnabled = false,
                              bool twemproxyEnabled = false,
                              string? strictCompatibilityModeVersion = null)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
        Check.EnsureNotNull(endpoints, nameof(endpoints));

        if (endpoints.Count == 0)
        {
            throw new InvalidOperationException(Resources.IDS_ENDPOINT_LIST_MUST_NOT_BE_EMPTY);
        }

        Key = key;
        Database = database;
        Endpoints = endpoints;
        Password = password;
        IsSsl = isSsl;
        SslHost = sslHost;
        ConnectionTimeout = connectionTimeout;
        AllowAdmin = allowAdmin;
        KeyspaceNotificationsEnabled = keyspaceNotificationsEnabled;
        TwemproxyEnabled = twemproxyEnabled;
        StrictCompatibilityModeVersion = strictCompatibilityModeVersion;

        _configurationOptions = CreateConfigurationOptions();

        // 稍后作为连接查找的键使用，并且该对象无论使用哪个构造函数都应保持一致
        ConnectionString = _configurationOptions.ToString();
    }

    /// <summary>
    /// 初始化 <see cref="RedisConfiguration"/> 类的新实例。
    /// </summary>
    /// <param name="key">
    /// 配置键，缓存句柄将用它来查找与缓存句柄名称对应的配置。
    /// </param>
    /// <param name="connectionString">
    /// 除了逐项指定所有属性外，也可以通过一个连接字符串完成配置。
    /// </param>
    /// <param name="database">要使用的 Redis 数据库。</param>
    /// <param name="keyspaceNotificationsEnabled">启用键空间通知，以便对项的逐出/过期做出反应。</param>
    /// <param name="strictCompatibilityModeVersion">
    /// 获取或设置版本号，用于限制缓存管理器可用的功能。
    /// </param>
    public RedisConfiguration(string key,
                              string connectionString,
                              int database = 0,
                              bool keyspaceNotificationsEnabled = false,
                              string? strictCompatibilityModeVersion = null)
    {
        Key = key;
        Database = database;
        KeyspaceNotificationsEnabled = keyspaceNotificationsEnabled;
        StrictCompatibilityModeVersion = strictCompatibilityModeVersion;
        ConnectionString = connectionString;
    }

    private ConfigurationOptions CreateConfigurationOptions()
    {
        var configurationOptions = new ConfigurationOptions()
        {
            AllowAdmin = AllowAdmin,
            ConnectTimeout = ConnectionTimeout,
            Password = Password,
            Ssl = IsSsl,
            SslHost = SslHost,
            ConnectRetry = 10,
            AbortOnConnectFail = false,
            Proxy = TwemproxyEnabled ? Proxy.Twemproxy : Proxy.None
        };

        foreach (var endpoint in Endpoints)
        {
            configurationOptions.EndPoints.Add(endpoint.Host, endpoint.Port);
        }

        return configurationOptions;
    }

    /// <summary>
    /// 获取此配置定义的 <see cref="StackExchange.Redis.ConfigurationOptions"/>。
    /// </summary>
    public ConfigurationOptions ConfigurationOptions => _configurationOptions;

    /// <summary>
    /// 获取或设置版本号，用于限制缓存管理器可用的功能。
    /// 例如将其设置为 <c>"2.4"</c> 可禁用 LUA 支持。
    /// </summary>
    /// <remarks>
    /// 当无法进行自动功能检测时，也可以使用此属性。例如使用 TwemProxy 时就是这样，
    /// 因为用于查询功能的服务器集合不可用/不受支持。
    /// CacheManager 默认会回退到支持 LUA 的版本。如果你在 TwemProxy 后面使用不支持 LUA 的
    /// Redis 服务器，请使用此属性！
    /// </remarks>
    public string? StrictCompatibilityModeVersion { get; set; }

    /// <summary>
    /// 获取或设置 Redis 配置的标识符。
    /// <para>
    /// 该标识符可能需要与缓存句柄的名称匹配，才能让缓存句柄使用此配置。
    /// </para>
    /// </summary>
    /// <value>
    /// 该键。
    /// </value>
    public string Key { get; set; } = null!;

    /// <summary>
    /// 获取或设置连接字符串。
    /// </summary>
    /// <value>
    /// 连接字符串。
    /// </value>
    public string ConnectionString
    {
        get => _connectionString;
        set
        {
            _configurationOptions = ConfigurationOptions.Parse(value, true);

            // 回读部分属性，以便在 RedisCacheHandle 中针对这些设置做出反应
            TwemproxyEnabled = _configurationOptions.Proxy == Proxy.Twemproxy;
            AllowAdmin = _configurationOptions.AllowAdmin;
            ConnectionTimeout = _configurationOptions.ConnectTimeout;
            IsSsl = _configurationOptions.Ssl;
            SslHost = _configurationOptions.SslHost;

            // 如果连接字符串中指定了默认数据库，则使用该数据库，否则使用显式的数据库设置。
            // 修复 #175
            Database = _configurationOptions.DefaultDatabase ?? Database;

            _connectionString = _configurationOptions.ToString();
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new ArgumentException(Resources.IDS_INVALID_REDIS_CONNECTION_STRING);
            }
        }
    }

    /// <summary>
    /// 获取或设置用于连接 Redis 服务器的密码。
    /// </summary>
    /// <value>
    /// 密码。
    /// </value>
    public string? Password { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示是否使用 SSL 加密。
    /// </summary>
    /// <value>
    ///   如果此实例使用 SSL 则为 <c>true</c>；否则为 <c>false</c>。
    /// </value>
    public bool IsSsl { get; set; }

    /// <summary>
    /// 获取或设置 SSL 主机。
    /// 如果设置，将在服务器证书上强制使用该特定主机。
    /// </summary>
    /// <value>
    /// SSL 主机。
    /// </value>
    public string? SslHost { get; set; }

    /// <summary>
    /// 获取或设置任何连接操作的超时时间。
    /// </summary>
    /// <value>
    /// 连接超时时间。
    /// </value>
    public int ConnectionTimeout { get; set; }

    /// <summary>
    /// 获取用于连接 Redis 服务器的终结点列表。
    /// </summary>
    /// <value>
    /// 终结点列表。
    /// </value>
    public IList<ServerEndPoint> Endpoints { get; } = new List<ServerEndPoint>();

    /// <summary>
    /// 获取或设置一个值，指示是否允许连接执行某些“有风险”的命令。
    /// <para><c>cache.Clear</c> 要求将其设置为 true，因为我们会清空 Redis 数据库。
    /// </para>
    /// </summary>
    /// <value>
    ///   如果允许“有风险”的命令则为 <c>true</c>；否则为 <c>false</c>。
    /// </value>
    public bool AllowAdmin { get; set; }

    /// <summary>
    /// 获取或设置缓存将使用的 Redis 数据库索引。
    /// </summary>
    /// <value>
    /// 数据库。
    /// </value>
    public int Database { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示 Redis 缓存句柄是否应使用键空间通知来响应
    /// Redis 的逐出或过期事件，然后将这些事件转发给缓存管理器。
    /// 技术细节请参阅 <see href="https://redis.io/topics/notifications"/>。
    /// <para>
    /// 要使用此功能，你可能需要在 Redis 服务器上启用该功能。
    /// </para>
    /// </summary>
    public bool KeyspaceNotificationsEnabled { get; set; }

    /// <summary>
    /// 获取或设置一个值，以指示是否正在使用 Twemproxy。
    /// </summary>
    public bool TwemproxyEnabled { get; set; }
}

/// <summary>
/// 定义一个终结点。
/// </summary>
public sealed class ServerEndPoint
{
    /// <summary>
    /// 初始化 <see cref="ServerEndPoint"/> 类的新实例。
    /// </summary>
    public ServerEndPoint()
    {
    }

    /// <summary>
    /// 初始化 <see cref="ServerEndPoint"/> 类的新实例。
    /// </summary>
    /// <param name="host">主机。</param>
    /// <param name="port">端口。</param>
    /// <exception cref="ArgumentNullException">如果主机为 null。</exception>
    public ServerEndPoint(string host, int port)
    {
        Check.EnsureNotNullOrWhiteSpace(host, nameof(host));

        Host = host;
        Port = port;
    }

    /// <summary>
    /// 获取或设置端口。
    /// </summary>
    /// <value>端口。</value>
    public int Port { get; set; }

    /// <summary>
    /// 获取或设置主机。
    /// </summary>
    /// <value>主机。</value>
    public string Host { get; set; } = null!;
}