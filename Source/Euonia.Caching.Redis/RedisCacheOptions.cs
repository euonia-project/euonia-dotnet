namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// The redis cache options.
/// </summary>
public class RedisCacheOptions
{
    /// <summary>
    /// Gets or sets the redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Gets or sets the maximum redis access retry times.
    /// <remarks>Default: 5.</remarks>
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the cache access retry timeout(ms).
    /// <remarks>Default: 3000.</remarks>
    /// </summary>
    public int RetryTimeout { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the redis cache update mode.
    /// <see cref="CacheUpdateMode"/>
    /// <value>None,Up</value>
    /// <remarks>Default: Up.</remarks>
    /// </summary>
    public CacheUpdateMode UpdateMode { get; set; } = CacheUpdateMode.Up;

    /// <summary>
    /// Gets or sets the redis database.
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Gets or sets the cache key prefix.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the cache expire time.
    /// </summary>
    public TimeSpan? Expires { get; set; }
}
