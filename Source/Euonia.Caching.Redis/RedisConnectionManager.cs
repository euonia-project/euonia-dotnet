using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

internal class RedisConnectionManager
{
    private static readonly Dictionary<string, IConnectionMultiplexer> _connections = new();
    private static readonly object _connectLock = new();

    private readonly string _connectionString;
    private readonly RedisConfiguration _configuration;

    public RedisConnectionManager(RedisConfiguration configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        Check.EnsureNotNullOrWhiteSpace(configuration.ConnectionString, nameof(RedisConfiguration.ConnectionString));

        _configuration = configuration;
        _connectionString = configuration.ConnectionString;
    }

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

    public IDatabase Database => Connect().GetDatabase(_configuration.Database);

    public ISubscriber Subscriber => Connect().GetSubscriber();

    public RedisFeatures Features
    {
        get
        {
            // new: if strict mode enabled, return the feature set supported by that version.
            if (!string.IsNullOrEmpty(_configuration.StrictCompatibilityModeVersion))
            {
                return new RedisFeatures(Version.Parse(_configuration.StrictCompatibilityModeVersion));
            }

            if (_configuration.TwemproxyEnabled)
            {
                // server features are not available, returning a default version...
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

    public IConnectionMultiplexer Connect()
    {
        IConnectionMultiplexer connection;
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
                    "Couldn't establish a connection for '{0}'.",
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

    private class LogWriter : StringWriter
    {
        public override void Write(char value)
        {
        }

        public override void Write(string value)
        {
        }

        public override void Write(char[] buffer, int index, int count)
        {
            var logValue = new string(buffer, index, count);
            Debug.WriteLine(logValue);
        }
    }
}