using Nerosoft.Euonia.Caching.Internal;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// Redis 的缓存句柄实现。
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
public class RedisCacheHandle<TValue> : BaseCacheHandle<TValue>
{
	// ReSharper disable once StaticMemberInGenericType
	private static readonly TimeSpan _minimumExpirationTimeout = TimeSpan.FromMilliseconds(1);
	private const string BASE64_PREFIX = "base64\0";
	private const string HASH_FIELD_CREATED = "created";
	private const string HASH_FIELD_EXPIRATION_MODE = "expiration";
	private const string HASH_FIELD_EXPIRATION_TIMEOUT = "timeout";
	private const string HASH_FIELD_TYPE = "type";
	private const string HASH_FIELD_VALUE = "value";
	private const string HASH_FIELD_VERSION = "version";
	private const string HASH_FIELD_USES_DEFAULT_EXP = "defaultExpiration";

	private const string SCRIPT_ADD = $@"
if redis.call('HSETNX', KEYS[1], '{HASH_FIELD_VALUE}', ARGV[1]) == 1 then
    local result=redis.call('HMSET', KEYS[1], '{HASH_FIELD_TYPE}', ARGV[2], '{HASH_FIELD_EXPIRATION_MODE}', ARGV[3], '{HASH_FIELD_EXPIRATION_TIMEOUT}', ARGV[4], '{HASH_FIELD_CREATED}', ARGV[5], '{HASH_FIELD_VERSION}', 1, '{HASH_FIELD_USES_DEFAULT_EXP}', ARGV[6])
    if ARGV[3] > '1' and ARGV[4] ~= '0' then
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
    else
        redis.call('PERSIST', KEYS[1])
    end
    return result
else
    return nil
end";

	private const string SCRIPT_PUT = $@"
local result=redis.call('HMSET', KEYS[1], '{HASH_FIELD_VALUE}', ARGV[1], '{HASH_FIELD_TYPE}', ARGV[2], '{HASH_FIELD_EXPIRATION_MODE}', ARGV[3], '{HASH_FIELD_EXPIRATION_TIMEOUT}', ARGV[4], '{HASH_FIELD_CREATED}', ARGV[5], '{HASH_FIELD_USES_DEFAULT_EXP}', ARGV[6])
redis.call('HINCRBY', KEYS[1], '{HASH_FIELD_VERSION}', 1)
if ARGV[3] > '1' and ARGV[4] ~= '0' then
    redis.call('PEXPIRE', KEYS[1], ARGV[4])
else
    redis.call('PERSIST', KEYS[1])
end
return result";

	// 脚本此时也应更新过期时间。如果是滑动过期，则更新滑动窗口
	private const string SCRIPT_UPDATE = $@"
if redis.call('HGET', KEYS[1], '{HASH_FIELD_VERSION}') == ARGV[2] then
    local result=redis.call('HSET', KEYS[1], '{HASH_FIELD_VALUE}', ARGV[1])
    redis.call('HINCRBY', KEYS[1], '{HASH_FIELD_VERSION}', 1)
    if ARGV[3] == '2' and ARGV[4] ~= '0' then
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
    end
    return result;
else
    return nil
end";

	private const string SCRIPT_GET = $@"
local result = redis.call('HMGET', KEYS[1], '{HASH_FIELD_VALUE}', '{HASH_FIELD_EXPIRATION_MODE}', '{HASH_FIELD_EXPIRATION_TIMEOUT}', '{HASH_FIELD_CREATED}', '{HASH_FIELD_TYPE}', '{HASH_FIELD_VERSION}', '{HASH_FIELD_USES_DEFAULT_EXP}')
if (result[2] and result[2] == '2') then
    if (result[3] and result[3] ~= '' and result[3] ~= '0') then
        redis.call('PEXPIRE', KEYS[1], result[3])
    end
end
return result";

	private readonly Dictionary<ScriptType, LoadedLuaScript> _shaScripts = new();
	private readonly Dictionary<ScriptType, LuaScript> _luaScripts = new();
	private readonly CacheManagerConfiguration _managerConfiguration;
	private readonly RedisValueConverter _valueConverter;
	private readonly RedisConnectionManager _connection;
	private readonly bool _isLuaAllowed;
	private bool _canPreloadScripts;
	private readonly RedisConfiguration _redisConfiguration;

	// 标记脚本是否已初始加载到服务器
	private bool _scriptsLoaded;

	private readonly object _lockObject = new();

	/// <summary>
	/// 初始化 <see cref="RedisCacheHandle{TCacheValue}"/> 类的新实例。
	/// </summary>
	/// <param name="managerConfiguration">管理器配置。</param>
	/// <param name="configuration">缓存句柄配置。</param>
	/// <param name="connectionString">Redis 连接字符串，不能为 <see langword="null"/> 或空白。</param>
	public RedisCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, string connectionString)
		: base(managerConfiguration, configuration)
	{
		Check.EnsureNotNull(managerConfiguration, nameof(managerConfiguration));
		Check.EnsureNotNull(configuration, nameof(configuration));
		Check.EnsureNotNullOrWhiteSpace(connectionString, nameof(connectionString));

		_managerConfiguration = managerConfiguration;
		_valueConverter = new RedisValueConverter();
		_redisConfiguration = RedisConfigurations.GetConfiguration(configuration.Key, connectionString);
		_connection = new RedisConnectionManager(_redisConfiguration);
		_isLuaAllowed = _connection.Features.Scripting;

		// 如果处于 twemproxy 模式，立即禁用预加载，因为该模式不支持预加载。
		_canPreloadScripts = !_redisConfiguration.TwemproxyEnabled;

		if (_redisConfiguration.KeyspaceNotificationsEnabled)
		{
			// notify-keyspace-events 至少需要设置为 "Exe"！否则我们将接收不到任何事件。
			// 该配置必须按服务器分别设置，而且不应自动完成，因为这需要管理员权限！
			// 至少尝试检查这些设置是否已配置（不过该检查仅在 useAdmin 设为 true 时才有效）。
			//try
			//{
			//    var configurations = _connection.GetConfiguration("notify-keyspace-events");
			//    foreach (var cfg in configurations)
			//    {
			//        if (!cfg.Value.Contains("E"))
			//        {
			//            Logger.LogWarn("Server {0} is missing configuration value 'E' in notify-keyspace-events to enable keyevents.", cfg.Key);
			//        }

			//        if (!(cfg.Value.Contains("A") ||
			//            (cfg.Value.Contains("x") && cfg.Value.Contains("e"))))
			//        {
			//            Logger.LogWarn("Server {0} is missing configuration value 'A' or 'x' and 'e' in notify-keyspace-events to enable keyevents for expired and evicted keys.", cfg.Key);
			//        }
			//    }
			//}
			//catch
			//{
			//    Logger.LogDebug("Could not read configuration from redis to validate notify-keyspace-events. Most likely useAdmin is not set to true.");
			//}

			SubscribeKeyspaceNotifications();
		}
	}

	/// <inheritdoc />
	public override bool IsDistributedCache => true;

	/// <summary>
	/// 获取该缓存句柄当前维护的项数。
	/// </summary>
	/// <value>项数。</value>
	/// <exception cref="InvalidOperationException">未找到活动的主节点。</exception>
	public override int Count
	{
		get
		{
			if (_redisConfiguration.TwemproxyEnabled)
			{
				return 0;
			}

			var count = 0;
			foreach (var server in Servers.Where(p => !p.IsReplica && p.IsConnected))
			{
				count += (int)server.DatabaseSize(_redisConfiguration.Database);
			}

			// 近似大小，仅统计主节点上的数量..
			return count;
		}
	}

	/// <summary>
	/// 获取服务器。
	/// </summary>
	/// <value>服务器列表。</value>
	public IEnumerable<IServer> Servers => _connection.Servers;

	/// <summary>
	/// 获取 Redis 服务器支持的功能。
	/// </summary>
	/// <value>服务器功能。</value>
	public RedisFeatures Features => _connection.Features;

	/// <summary>
	/// 获取一个值，指示是否可以使用 Lua 实现来代替手动实现。
	/// 该标志会根据 Redis 服务器版本通过功能检测自动设置，
	/// 或者通过 <see cref="RedisConfiguration.StrictCompatibilityModeVersion"/> 设置为不支持 Lua 脚本的版本时设置。
	/// </summary>
	public bool IsLuaAllowed => _isLuaAllowed;

	/// <summary>
	/// 清空此缓存，移除基础缓存和所有区域中的所有项。
	/// </summary>
	/// <remarks>
	/// <b>注意爆炸半径</b>：实现为对当前逻辑库执行 <c>FLUSHDB</c>，清空的是**整个数据库**，
	/// 而不是「本缓存」——同一逻辑库中其他应用的键、以及其他缓存类型的条目都会被一并删除
	/// （例如 <c>Euonia.Concurrency.Redis</c> 的分布式锁键）。
	/// <para>
	/// 该 handle 没有自己的键空间前缀（键前缀由上层 <c>BaseCacheService.RewriteKey</c> 添加，句柄无从得知），
	/// 因此无法把删除限定到自身条目。生产环境请为缓存使用独立的逻辑库（<c>Database</c>）。
	/// </para>
	/// <para>
	/// <c>FLUSHDB</c> 需要连接启用 <c>allowAdmin</c>；未启用时 Redis 会拒绝执行，
	/// 此时抛出带有可操作说明的 <see cref="NotSupportedException"/>。
	/// </para>
	/// </remarks>
	/// <exception cref="NotSupportedException">当连接未启用 <c>allowAdmin</c> 而无法执行 <c>FLUSHDB</c> 时抛出。</exception>
	public override void Clear()
	{
		try
		{
			foreach (var server in Servers.Where(p => !p.IsReplica))
			{
				Retry(() =>
				{
					if (server.IsConnected)
					{
						server.FlushDatabase(_redisConfiguration.Database);
					}
				});
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			// 此前只捕获 NotSupportedException，但 StackExchange.Redis 实际抛出的是
			// RedisCommandException（"This operation is not available unless admin mode is enabled: FLUSHDB"），
			// 于是给用户看的可操作提示从不出现，只有一条原始驱动错误。
			throw new NotSupportedException(
				string.Format(Resources.IDS_CLEAR_REQUIRES_ALLOW_ADMIN, _redisConfiguration.Database, exception.Message),
				exception);
		}
	}

	/// <summary>
	/// 清空缓存区域，仅移除指定 <paramref name="region"/> 中的所有项。
	/// </summary>
	/// <param name="region">缓存区域。</param>
	public override void ClearRegion(string region)
	{
		Retry(() =>
		{
			// 我们将区域中存储的所有键都存放在 key=region 的哈希中
			var hashKeys = _connection.Database.HashKeys(region);

			if (hashKeys.Length > 0)
			{
				// 现在移除该区域中的所有键
				// 01/32/16 改为逐个移除，因为在集群上这些键可能属于多个槽
				foreach (var key in hashKeys.Where(p => p.HasValue))
				{
					_connection.Database.KeyDelete(key.ToString(), CommandFlags.FireAndForget);
				}
			}

			// 现在删除该区域
			_connection.Database.KeyDelete(region);
		});
	}

	/// <inheritdoc />
	public override bool Exists(string key)
	{
		var fullKey = GetKey(key);
		return Retry(() => _connection.Database.KeyExists(fullKey));
	}

	/// <inheritdoc />
	public override bool Exists(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		var fullKey = GetKey(key, region);
		return Retry(() => _connection.Database.KeyExists(fullKey));
	}

	/// <inheritdoc />
	public override CacheItemUpdateResult<TValue> Update(string key, Func<TValue, TValue> updateValue, int maxRetries)
		=> Update(key, null!, updateValue, maxRetries);

	/// <inheritdoc />
	public override CacheItemUpdateResult<TValue> Update(string key, string region, Func<TValue, TValue> updateValue, int maxRetries)
	{
		if (!_isLuaAllowed)
		{
			return UpdateNoScript(key, region, updateValue, maxRetries);
		}

		var tries = 0;
		var fullKey = GetKey(key, region);

		return Retry(() =>
		{
			do
			{
				tries++;

				var item = GetCacheItemAndVersion(key, region, out int version);

				if (item == null)
				{
					return CacheItemUpdateResult.ForItemDidNotExist<TValue>();
				}

				ValidateExpirationTimeout(item);

				// 执行更新
				var newValue = updateValue(item.Value);

				// 增加 null 检查，显式抛出以便更一致。否则会在脚本执行内部抛出
				if (newValue == null)
				{
					return CacheItemUpdateResult.ForFactoryReturnedNull<TValue>();
				}

				// 更新时也重置 TTL
				var result = Eval(ScriptType.Update, fullKey, new[]
				{
					ToRedisValue(newValue),
					version,
					(int)item.ExpirationMode,
					(long)item.ExpirationTimeout.TotalMilliseconds,
				});

				if (result is { IsNull: false })
				{
					// 优化：更新后不再重新获取该项（它也可能已经再次发生变化）
					var newItem = item.WithValue(newValue);
					newItem.LastAccessedUtc = DateTime.UtcNow;

					return CacheItemUpdateResult.ForSuccess(newItem, tries > 1, tries);
				}
			}
			while (tries <= maxRetries);

			return CacheItemUpdateResult.ForTooManyRetries<TValue>(tries);
		});
	}

	private CacheItemUpdateResult<TValue> UpdateNoScript(string key, string region, Func<TValue, TValue> updateValue, int maxRetries)
	{
		bool committed;
		var tries = 0;
		var fullKey = GetKey(key, region);

		return Retry(() =>
		{
			do
			{
				tries++;

				var item = GetCacheItemInternal(key, region);

				if (item == null)
				{
					return CacheItemUpdateResult.ForItemDidNotExist<TValue>();
				}

				ValidateExpirationTimeout(item);

				var oldValue = ToRedisValue(item.Value);

				var tran = _connection.Database.CreateTransaction();
				tran.AddCondition(Condition.HashEqual(fullKey, HASH_FIELD_VALUE, oldValue));

				// 执行更新
				var newValue = updateValue(item.Value);

				// 增加 null 检查，显式抛出以便更一致。否则会在稍后抛出
				if (newValue == null)
				{
					return CacheItemUpdateResult.ForFactoryReturnedNull<TValue>();
				}

				tran.HashSetAsync(fullKey, HASH_FIELD_VALUE, ToRedisValue(newValue));

				committed = tran.Execute();

				if (committed)
				{
					var newItem = item.WithValue(newValue);
					newItem.LastAccessedUtc = DateTime.UtcNow;

					if (newItem.ExpirationMode == CacheExpirationMode.Sliding && newItem.ExpirationTimeout != TimeSpan.Zero)
					{
						_connection.Database.KeyExpire(fullKey, newItem.ExpirationTimeout, CommandFlags.FireAndForget);
					}

					return CacheItemUpdateResult.ForSuccess(newItem, tries > 1, tries);
				}
			}
			while (committed == false && tries <= maxRetries);

			return CacheItemUpdateResult.ForTooManyRetries<TValue>(tries);
		});
	}

	/// <summary>
	/// 向缓存添加一个值。
	/// <para>
	/// Add 调用是同步的，因此可能比即发即忘的 Put 慢，但我们希望在操作成功与否时
	/// 返回 true|false。如果项已存在，始终返回 true 会产生误导
	/// </para>
	/// </summary>
	/// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
	/// <returns>
	/// 如果键尚未添加到缓存则返回 <c>true</c>，否则返回 <c>false</c>。
	/// </returns>
	protected override bool AddInternalPrepared(CacheItem<TValue> item) =>
		Retry(() => Set(item, When.NotExists, true));

	/// <summary>
	/// 执行与释放、归还或重置非托管资源相关的应用程序定义任务。
	/// </summary>
	/// <param name="disposeManaged">指示是否应释放托管资源。</param>
	protected override void Dispose(bool disposeManaged)
	{
		base.Dispose(disposeManaged);
		if (disposeManaged)
		{
			// this.connection.RemoveConnection();
		}
	}

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <returns>该 <c>CacheItem</c>。</returns>
	protected override CacheItem<TValue>? GetCacheItemInternal(string key)
		=> GetCacheItemInternal(key, null);

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>该 <c>CacheItem</c>。</returns>
	protected override CacheItem<TValue>? GetCacheItemInternal(string key, string? region)
	{
		return GetCacheItemAndVersion(key, region, out _);
	}

	private CacheItem<TValue>? GetCacheItemAndVersion(string key, string? region, out int version)
	{
		version = -1;
		if (!_isLuaAllowed)
		{
			return GetCacheItemInternalNoScript(key, region);
		}

		var fullKey = GetKey(key, region);

		var result = Retry(() => Eval(ScriptType.Get, fullKey));
		if (result == null || result.IsNull)
		{
			// 出了问题。HMGET 至少应为每个请求的字段返回 null 结果
			throw new InvalidOperationException(string.Format(Resources.IDS_ERROR_RETRIEVING, fullKey));
		}

		var values = ((RedisValue[]?)result)!;

		// 第一项存储值
		var item = values![0];
		var expirationModeItem = values[1];
		var timeoutItem = values[2];
		var createdItem = values[3];
		var valueTypeItem = values[4];
		version = (int)values[5];
		var usesDefaultExpiration = values[6].HasValue
			? (bool)values[6] // 如果设置了标志，则一切正常...
			: !expirationModeItem.HasValue || !timeoutItem.HasValue; // 否则回退为使用配置中的默认过期时间

		// 注意不能把空字符串/空字节数组当作“不存在”，否则 Put(key, "") 后 Get 会返回 null。
		if (!item.HasValue || !valueTypeItem.HasValue /* 被部分移除？ */ || item.IsNull)
		{
			return null;
		}

		var expirationMode = CacheExpirationMode.None;
		var expirationTimeout = default(TimeSpan);

		// 检查是否在哈希上设置了过期模式
		if (expirationModeItem.HasValue && timeoutItem.HasValue)
		{
			if (!timeoutItem.IsNullOrEmpty && !expirationModeItem.IsNullOrEmpty)
			{
				expirationMode = (CacheExpirationMode)(int)expirationModeItem;
				expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
			}
			else
			{
			}
		}

		var value = FromRedisValue(item, valueTypeItem.ToString());

		var cacheItem =
			usesDefaultExpiration ? string.IsNullOrWhiteSpace(region) ? new CacheItem<TValue>(key, value) : new CacheItem<TValue>(key, region, value) :
			string.IsNullOrWhiteSpace(region) ? new CacheItem<TValue>(key, value, expirationMode, expirationTimeout) :
			new CacheItem<TValue>(key, region, value, expirationMode, expirationTimeout);

		if (createdItem.HasValue)
		{
			cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem, DateTimeKind.Utc));
		}

		if (cacheItem.IsExpired)
		{
			TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, cacheItem.Value);

			return null;
		}

		return cacheItem;
	}

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>，不使用 Lua 脚本。
	/// </summary>
	/// <param name="key">缓存项的键。</param>
	/// <param name="region">缓存区域名称；为 <see langword="null"/> 时不使用区域。</param>
	/// <returns>找到的缓存项；未找到或已过期时为 <see langword="null"/>。</returns>
	protected virtual CacheItem<TValue>? GetCacheItemInternalNoScript(string key, string? region)
	{
		return Retry(() =>
		{
			var fullKey = GetKey(key, region);

			// 同时获取值和过期模式（如果存在）。如果已设置过期模式
			// 且为滑动过期，我们稍后还会获取超时时间
			var values = _connection.Database.HashGet(
				fullKey,
				new RedisValue[]
				{
					HASH_FIELD_VALUE,
					HASH_FIELD_EXPIRATION_MODE,
					HASH_FIELD_EXPIRATION_TIMEOUT,
					HASH_FIELD_CREATED,
					HASH_FIELD_TYPE,
					HASH_FIELD_USES_DEFAULT_EXP
				});

			// 第一项存储值
			var item = values[0];
			var expirationModeItem = values[1];
			var timeoutItem = values[2];
			var createdItem = values[3];
			var valueTypeItem = values[4];
			var usesDefaultExpiration = values[5].HasValue
				? (bool)values[5] // 如果设置了标志，则一切正常...
				: !expirationModeItem.HasValue || !timeoutItem.HasValue; // 否则回退为使用配置中的默认过期时间

			if (!item.HasValue || !valueTypeItem.HasValue /* 被部分移除？ */
			                   || item.IsNull)
			{
				return null;
			}

			var expirationMode = CacheExpirationMode.None;
			var expirationTimeout = default(TimeSpan);

			// 检查是否在哈希上设置了过期模式
			if (expirationModeItem.HasValue && timeoutItem.HasValue)
			{
				// 增加对空字符串结果的合理性检查。可能出现在像 #74 这样的罕见情况中
				if (!timeoutItem.IsNullOrEmpty && !expirationModeItem.IsNullOrEmpty)
				{
					expirationMode = (CacheExpirationMode)(int)expirationModeItem;
					expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
				}
				else
				{
				}
			}

			var value = FromRedisValue(item, valueTypeItem.ToString());

			var cacheItem =
				usesDefaultExpiration ? string.IsNullOrWhiteSpace(region) ? new CacheItem<TValue>(key, value) : new CacheItem<TValue>(key, region, value) :
				string.IsNullOrWhiteSpace(region) ? new CacheItem<TValue>(key, value, expirationMode, expirationTimeout) :
				new CacheItem<TValue>(key, region, value, expirationMode, expirationTimeout);

			if (createdItem.HasValue)
			{
				cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem, DateTimeKind.Utc));
			}

			if (cacheItem.IsExpired)
			{
				TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, cacheItem.Value);

				return null;
			}

			// 更新滑动过期
			if (expirationMode == CacheExpirationMode.Sliding && expirationTimeout != default)
			{
				_connection.Database.KeyExpire(fullKey, cacheItem.ExpirationTimeout, CommandFlags.FireAndForget);
			}

			return cacheItem;
		});
	}

	/// <summary>
	/// 将 <paramref name="item"/> 放入缓存。如果该项已存在，将用新值更新它。
	/// 如果该项不存在，则会将其添加到缓存。
	/// </summary>
	/// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
	protected override void PutInternalPrepared(CacheItem<TValue> item) =>
		Retry(() => Set(item, When.Always));

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <returns>
	/// 如果键被找到并从缓存中移除则返回 <c>true</c>，否则返回 <c>false</c>。
	/// </returns>
	protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 如果键被找到并从缓存中移除则返回 <c>true</c>，否则返回 <c>false</c>。
	/// </returns>
	protected override bool RemoveInternal(string key, string? region)
	{
		return Retry(() =>
		{
			var fullKey = GetKey(key, region);

			// 清理区域
			if (!string.IsNullOrWhiteSpace(region))
			{
				_connection.Database.HashDelete(region, fullKey, CommandFlags.FireAndForget);
			}

			// 移除键
			var result = _connection.Database.KeyDelete(fullKey);

			return result;
		});
	}

	private void SubscribeKeyspaceNotifications()
	{
		_connection.Subscriber.Subscribe(
			new RedisChannel($"__keyevent@{_redisConfiguration.Database}__:expired", RedisChannel.PatternMode.Auto),
			(_, key) =>
			{
				var tuple = ParseKey(key);
				// 我们无法在此返回原始值，因为拿不到它
				TriggerCacheSpecificRemove(tuple.Item1!, tuple.Item2, CacheItemRemovedReason.Expired, null);
			});

		_connection.Subscriber.Subscribe(
			new RedisChannel($"__keyevent@{_redisConfiguration.Database}__:evicted", RedisChannel.PatternMode.Auto),
			(_, key) =>
			{
				var tuple = ParseKey(key);

				// 我们无法在此返回原始值，因为拿不到它
				TriggerCacheSpecificRemove(tuple.Item1!, tuple.Item2, CacheItemRemovedReason.Evicted, null);
			});

		_connection.Subscriber.Subscribe(
			new RedisChannel($"__keyevent@{_redisConfiguration.Database}__:del", RedisChannel.PatternMode.Auto),
			(_, key) =>
			{
				var tuple = ParseKey(key);

				// 我们无法在此返回原始值，因为拿不到它
				TriggerCacheSpecificRemove(tuple.Item1!, tuple.Item2, CacheItemRemovedReason.ExternalDelete, null);
			});
	}

	private static Tuple<string?, string?> ParseKey(string? value)
	{
		if (value == null)
		{
			return Tuple.Create<string?, string?>(null, null);
		}

		var sepIndex = value.IndexOf(':');
		var hasRegion = sepIndex > 0;
		var key = value;
		string? region = null;

		if (hasRegion)
		{
			region = value[..sepIndex];
			key = value[(sepIndex + 1)..];

			if (region.StartsWith(BASE64_PREFIX))
			{
				region = region[BASE64_PREFIX.Length..];
				region = Encoding.UTF8.GetString(Convert.FromBase64String(region));
			}
		}

		if (key.StartsWith(BASE64_PREFIX))
		{
			key = key[BASE64_PREFIX.Length..];
			key = Encoding.UTF8.GetString(Convert.FromBase64String(key));
		}

		return Tuple.Create<string?, string?>(key, region);
	}

	private static void ValidateExpirationTimeout(CacheItem<TValue> item)
	{
		if ((item.ExpirationMode == CacheExpirationMode.Absolute || item.ExpirationMode == CacheExpirationMode.Sliding) && item.ExpirationTimeout < _minimumExpirationTimeout)
		{
			throw new ArgumentException(Resources.IDS_TIMEOUT_LOWER_THAN_ONE_MILLISECOND_NOT_SUPPORTED, nameof(item.ExpirationTimeout));
		}
	}

	private string GetKey(string key, string? region = null)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentNullException(nameof(key));
		}

		// 对于通知，我们必须从存储在 Redis 中的键还原出键和区域。
		// 如果键和/或区域本身包含分隔符，就无法做到这一点...
		// 因此，仅在启用该功能时才编码键和/或区域，
		// ParseKey 方法也会遵循该约定并解码键和/或区域。
		if (_redisConfiguration.KeyspaceNotificationsEnabled && key.Contains(":"))
		{
			key = BASE64_PREFIX + Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
		}

		var fullKey = key;

		if (!string.IsNullOrWhiteSpace(region))
		{
			if (_redisConfiguration.KeyspaceNotificationsEnabled && region.Contains(":"))
			{
				region = BASE64_PREFIX + Convert.ToBase64String(Encoding.UTF8.GetBytes(region));
			}

			fullKey = string.Concat(region, ":", key);
		}

		return fullKey;
	}

	private TValue FromRedisValue(RedisValue value, string valueType)
	{
		if (value.IsNull || !value.HasValue)
		{
			return default!;
		}

		if (_valueConverter is IRedisValueConverter<TValue> typedConverter)
		{
			return typedConverter.FromRedisValue(value, valueType);
		}

		return _valueConverter.FromRedisValue<TValue>(value, valueType);
	}

	private RedisValue ToRedisValue(TValue value)
	{
		if (_valueConverter is IRedisValueConverter<TValue> typedConverter)
		{
			return typedConverter.ToRedisValue(value);
		}

		return _valueConverter.ToRedisValue(value);
	}

	private T Retry<T>(Func<T> action) =>
		RetryHelper.Retry(action, _managerConfiguration.RetryTimeout, _managerConfiguration.MaxRetries);

	private void Retry(Action action)
		=> Retry(
			() =>
			{
				action();
				return true;
			});

	private bool Set(CacheItem<TValue> item, When when, bool sync = false)
	{
		if (!_isLuaAllowed)
		{
			return SetNoScript(item, when, sync);
		}

		var fullKey = GetKey(item.Key, item.Region);
		var value = ToRedisValue(item.Value);

		var flags = sync ? CommandFlags.None : CommandFlags.FireAndForget;

		ValidateExpirationTimeout(item);

		// ARGV [1]: 值，[2]: 类型，[3]: 过期模式，[4]: 过期超时（毫秒），[5]: 创建时间（计时周期）
		var parameters = new RedisValue[]
		{
			value,
			item.ValueType.AssemblyQualifiedName,
			(int)item.ExpirationMode,
			(long)item.ExpirationTimeout.TotalMilliseconds,
			item.CreatedUtc.Ticks,
			item.UsesExpirationDefaults
		};

		RedisResult result;
		if (when == When.NotExists)
		{
			result = Eval(ScriptType.Add, fullKey, parameters, flags);
		}
		else
		{
			result = Eval(ScriptType.Put, fullKey, parameters, flags);
		}

		// StackExchange.Redis 在 FireAndForget 下返回的不是 null，而是 IsNull == true 的 RedisResult。
		if (result == null || result.IsNull)
		{
			if (flags.HasFlag(CommandFlags.FireAndForget))
			{
				if (!string.IsNullOrWhiteSpace(item.Region))
				{
					// 如果使用了区域，则设置区域查找键
					_connection.Database.HashSet(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
				}

				// put 通过即发即忘方式运行，因此不会得到结果返回
				return true;
			}

			if (when == When.NotExists)
			{
				return false;
			}

			// 绝不应发生，脚本出了问题
			throw new InvalidOperationException(Resources.IDS_ADD_ITEM_RESULT_MUST_NOT_BE_NULL);
		}
		else
		{
			var resultValue = (RedisValue)result;

			if (resultValue.HasValue && resultValue.ToString().Equals("OK", StringComparison.OrdinalIgnoreCase))
			{
				// 添加成功：
				if (!string.IsNullOrWhiteSpace(item.Region))
				{
					// 如果使用了区域，则设置区域查找键
					// 我们无法在 Lua 中完成该操作，因为区域可能位于另一个集群节点上！
					_connection.Database.HashSet(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
				}

				return true;
			}

			return false;
		}
	}

	private bool SetNoScript(CacheItem<TValue> item, When when, bool sync = false)
	{
		return Retry(() =>
		{
			var fullKey = GetKey(item.Key, item.Region);
			var value = ToRedisValue(item.Value);

			ValidateExpirationTimeout(item);

			var metaValues = new[]
			{
				new HashEntry(HASH_FIELD_TYPE, item.ValueType.AssemblyQualifiedName),
				new HashEntry(HASH_FIELD_EXPIRATION_MODE, (int)item.ExpirationMode),
				new HashEntry(HASH_FIELD_EXPIRATION_TIMEOUT, (long)item.ExpirationTimeout.TotalMilliseconds),
				new HashEntry(HASH_FIELD_CREATED, item.CreatedUtc.Ticks),
				new HashEntry(HASH_FIELD_USES_DEFAULT_EXP, item.UsesExpirationDefaults)
			};

			var flags = sync ? CommandFlags.None : CommandFlags.FireAndForget;

			var setResult = _connection.Database.HashSet(fullKey, HASH_FIELD_VALUE, value, when, flags);

			// 即发即忘的 setResult 始终为 false，因此只能假定它成功...
			setResult = flags == CommandFlags.FireAndForget || setResult;

			if (!setResult)
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(item.Region))
			{
				// 如果使用了区域，则设置区域查找键
				_connection.Database.HashSet(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
			}

			// 设置额外字段，以备需要使用滑动过期。在这种情况下，
			// 我们还必须将过期模式和超时时间存储在哈希上，
			// 以便每次执行 get 时都能延长过期时间
			_connection.Database.HashSet(fullKey, metaValues, flags);

			if (item.ExpirationMode != CacheExpirationMode.None && item.ExpirationMode != CacheExpirationMode.Default)
			{
				_connection.Database.KeyExpire(fullKey, item.ExpirationTimeout, CommandFlags.FireAndForget);
			}
			else
			{
				// 修复 #9
				_connection.Database.KeyPersist(fullKey, CommandFlags.FireAndForget);
			}

			return true;
		});
	}

	private RedisResult Eval(ScriptType scriptType, RedisKey redisKey, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
	{
		if (!_scriptsLoaded)
		{
			lock (_lockObject)
			{
				if (!_scriptsLoaded)
				{
					LoadScripts();
					_scriptsLoaded = true;
				}
			}
		}

		LoadedLuaScript? script = null;
		if (!_luaScripts.TryGetValue(scriptType, out LuaScript? luaScript)
		    || (_canPreloadScripts && !_shaScripts.TryGetValue(scriptType, out script)))
		{
			_scriptsLoaded = false;
			throw new InvalidOperationException(Resources.IDS_LUA_SCRIPTS_NOT_LOADED);
		}

		try
		{
			if (_canPreloadScripts && script != null)
			{
				return _connection.Database.ScriptEvaluate(script.Hash, new[] { redisKey }, values, flags);
			}
			else
			{
				return _connection.Database.ScriptEvaluate(luaScript.ExecutableScript, new[] { redisKey }, values, flags);
			}
		}
		catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
		{
			LoadScripts();

			// 重试
			throw;
		}
	}

	private void LoadScripts()
	{
		lock (_lockObject)
		{
			var putLua = LuaScript.Prepare(SCRIPT_PUT);
			var addLua = LuaScript.Prepare(SCRIPT_ADD);
			var updateLua = LuaScript.Prepare(SCRIPT_UPDATE);
			var getLua = LuaScript.Prepare(SCRIPT_GET);
			_luaScripts.Clear();
			_luaScripts.Add(ScriptType.Add, addLua);
			_luaScripts.Add(ScriptType.Put, putLua);
			_luaScripts.Add(ScriptType.Update, updateLua);
			_luaScripts.Add(ScriptType.Get, getLua);

			// 服务器功能可能已被禁用
			if (_canPreloadScripts)
			{
				try
				{
					foreach (var server in Servers)
					{
						if (server.IsConnected)
						{
							_shaScripts[ScriptType.Put] = putLua.Load(server);
							_shaScripts[ScriptType.Add] = addLua.Load(server);
							_shaScripts[ScriptType.Update] = updateLua.Load(server);
							_shaScripts[ScriptType.Get] = getLua.Load(server);
						}
					}
				}
				catch (NotSupportedException)
				{
					_canPreloadScripts = false;
				}
			}
		}
	}
}