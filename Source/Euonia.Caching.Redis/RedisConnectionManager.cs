using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// 管理 Redis 连接复用与服务器配置读写：按连接字符串缓存 <see cref="IConnectionMultiplexer"/>，并暴露数据库、订阅者与服务器能力集。
/// </summary>
internal class RedisConnectionManager
{
    private static readonly Dictionary<string, IConnectionMultiplexer> _connections = new();
    private static readonly object _connectLock = new();

    private readonly string _connectionString;
    private readonly RedisConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="RedisConnectionManager"/> 的新实例。
    /// </summary>
    /// <param name="configuration">Redis 连接配置，其连接字符串不能为空白。</param>
    public RedisConnectionManager(RedisConfiguration configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        Check.EnsureNotNullOrWhiteSpace(configuration.ConnectionString, nameof(RedisConfiguration.ConnectionString));

        _configuration = configuration;
        _connectionString = configuration.ConnectionString;
    }

    /// <summary>
    /// 获取当前连接涉及的所有 Redis 服务器端点。
    /// </summary>
    /// <value>已连接多路复用器上的服务器序列，枚举时逐项解析端点。</value>
    public IEnumerable<IServer> Servers
    {
        get
        {
            var endpoints = Connect().GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = Connect().GetServer(endpoint);
                yield return server;
            }
        }
    }

    /// <summary>
    /// 获取配置指定的 Redis 数据库。
    /// </summary>
    /// <value>按 <see cref="RedisConfiguration.Database"/> 选择的数据库。</value>
    public IDatabase Database => Connect().GetDatabase(_configuration.Database);

    /// <summary>
    /// 获取用于发布/订阅的 Redis 订阅者。
    /// </summary>
    /// <value>当前连接的多路复用器提供的订阅者。</value>
    public ISubscriber Subscriber => Connect().GetSubscriber();

    /// <summary>
    /// 获取用于版本判定的 Redis 功能集。
    /// </summary>
    /// <value>
    /// 严格兼容模式下为指定版本的功能集；Twemproxy 场景下为 3.0 的默认功能集；
    /// 否则取首个已连接服务器上报的功能集。
    /// </value>
    /// <exception cref="InvalidOperationException">当没有已连接的服务器可用时抛出。</exception>
    public RedisFeatures Features
    {
        get
        {
            // 新增：如果启用了严格模式，则返回该版本支持的功能集。
            if (!string.IsNullOrEmpty(_configuration.StrictCompatibilityModeVersion))
            {
                return new RedisFeatures(Version.Parse(_configuration.StrictCompatibilityModeVersion));
            }

            if (_configuration.TwemproxyEnabled)
            {
                // 服务器功能不可用，返回默认版本……
                return new RedisFeatures(Version.Parse("3.0"));
            }

            var server = Servers.FirstOrDefault(p => p.IsConnected);
            if (server == null)
            {
                throw new InvalidOperationException(Resources.IDS_NO_SERVERS_CONNECTED_OR_CONFIGURED);
            }

            return server.Features;
        }
    }

    /// <summary>
    /// 读取所有服务器上指定配置项的值。
    /// </summary>
    /// <param name="key">Redis 配置项名称。</param>
    /// <returns>端点与配置值的映射；不包含未提供该配置项的服务器。</returns>
    public Dictionary<System.Net.EndPoint, string> GetConfiguration(string key)
    {
        var result = new Dictionary<System.Net.EndPoint, string>();
        foreach (var server in Servers)
        {
            var values = server.ConfigGet(key).ToDictionary(k => k.Key, v => v.Value);

            if (values.ContainsKey(key))
            {
                var value = values.FirstOrDefault(p => p.Key == key);
                result.Add(server.EndPoint, value.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// 在所有服务器上设置指定配置项；值未变化时跳过写入。
    /// </summary>
    /// <param name="key">Redis 配置项名称。</param>
    /// <param name="value">要写入的配置值。</param>
    /// <param name="addValue">为 <see langword="true"/> 时把值追加到现有值之后，否则覆盖现有值。</param>
    /// <exception cref="InvalidOperationException">任一服务器设置失败时抛出，内部异常保留原始错误。</exception>
    public void SetConfigurationAllServers(string key, string value, bool addValue)
    {
        try
        {
            foreach (var server in Servers)
            {
                var values = server.ConfigGet(key).ToDictionary(k => k.Key, v => v.Value);

                if (values.ContainsKey(key))
                {
                    var oldValue = values.First(p => p.Key == key).Value;

                    if (!oldValue.Equals(value))
                    {
                        server.ConfigSet(key, addValue ? oldValue + value : value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(Resources.IDS_FAILED_TO_SET_KEY_VALUE, key, value), ex);
        }
    }

    /// <summary>
    /// 登记连接字符串与多路复用器的映射；同一连接字符串已存在时保留原连接。
    /// </summary>
    /// <param name="connectionString">连接字符串，作为映射键。</param>
    /// <param name="connection">要登记的多路复用器。</param>
    public static void AddConnection(string connectionString, IConnectionMultiplexer connection)
    {
        lock (_connectLock)
        {
            if (!_connections.ContainsKey(connectionString))
            {
                _connections.Add(connectionString, connection);
            }
        }
    }

    /// <summary>
    /// 移除指定连接字符串的映射；不存在时不做任何处理。
    /// </summary>
    /// <param name="connectionString">要移除的连接字符串。</param>
    /// <remarks>仅移除登记关系，不释放多路复用器实例。</remarks>
    public static void RemoveConnection(string connectionString)
    {
        lock (_connectLock)
        {
            if (_connections.ContainsKey(connectionString))
            {
                _connections.Remove(connectionString);
            }
        }
    }

    /// <summary>
    /// 获取与当前连接字符串关联的多路复用器；不存在时建立连接并登记。
    /// </summary>
    /// <returns>已连接的多路复用器；同一连接字符串在进程内复用同一实例。</returns>
    /// <exception cref="InvalidOperationException">连接失败、没有可写端点，或登记后仍无法取得实例时抛出。</exception>
    /// <remarks>建立连接时会校验至少存在一个可写端点（Twemproxy 模式下跳过该检查）。</remarks>
    public IConnectionMultiplexer Connect()
    {
        IConnectionMultiplexer? connection;
        lock (_connectLock)
        {
            if (!_connections.TryGetValue(_connectionString, out connection))
            {
                lock (_connectLock)
                {
                    if (!_connections.TryGetValue(_connectionString, out connection))
                    {
                        connection = ConnectionMultiplexer.Connect(_connectionString, new LogWriter());

                        if (!connection.IsConnected)
                        {
                            connection.Dispose();
                            throw new InvalidOperationException(string.Format(Resources.IDS_CONNECTION_FAILED, RemoveCredentials(_connectionString)));
                        }

                        connection.ConnectionRestored += (_, args) =>
                        {
	                        Debug.WriteLine("Connection restored, type: '{0}', failure: '{1}'", args.ConnectionType, args.FailureType);
                            //_logger.LogInfo(args.Exception, "Connection restored, type: '{0}', failure: '{1}'", args.ConnectionType, args.FailureType);
                        };

                        if (!_configuration.TwemproxyEnabled)
                        {
                            var endpoints = connection.GetEndPoints();
                            if (!endpoints.Select(p => connection.GetServer(p))
                                          .Any(p => !p.IsReplica || p.AllowReplicaWrites))
                            {
                                throw new InvalidOperationException(Resources.IDS_NO_WRITABLE_ENDPOINT);
                            }
                        }

                        _connections.Add(_connectionString, connection);
                    }
                }
            }
        }

        if (connection == null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.IDS_CONNECTION_FAILED,
                    RemoveCredentials(_connectionString)));
        }

        return connection;
    }

    private static string RemoveCredentials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Regex.Replace(value, @"password\s*=\s*[^,]*", "password=****", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 将 StackExchange.Redis 的内部日志写入调试输出，并丢弃逐字符写入以避免噪声。
    /// </summary>
    private class LogWriter : StringWriter
    {
        public override void Write(char value)
        {
        }

        public override void Write(string? value)
        {
        }

        public override void Write(char[] buffer, int index, int count)
        {
            var logValue = new string(buffer, index, count);
            Debug.WriteLine(logValue);
        }
    }
}