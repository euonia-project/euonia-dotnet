namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// Redis 缓存选项。
/// </summary>
public class RedisCacheOptions
{
    /// <summary>
    /// 获取或设置 Redis 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// 获取或设置 Redis 访问的最大重试次数。
    /// <remarks>默认值：5。</remarks>
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// 获取或设置缓存访问的重试超时时间（毫秒）。
    /// <remarks>默认值：3000。</remarks>
    /// </summary>
    public int RetryTimeout { get; set; } = 3000;

    /// <summary>
    /// 获取或设置 Redis 缓存更新模式。
    /// <see cref="CacheUpdateMode"/>
    /// <value>None,Up</value>
    /// <remarks>默认值：Up。</remarks>
    /// </summary>
    public CacheUpdateMode UpdateMode { get; set; } = CacheUpdateMode.Up;

    /// <summary>
    /// 获取或设置 Redis 数据库。
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// 获取或设置缓存键前缀。
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// 获取或设置缓存过期时间。
    /// </summary>
    public TimeSpan? Expires { get; set; }
}
