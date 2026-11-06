namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// The memory cache options.
/// </summary>
public class MemoryCacheOptions : Microsoft.Extensions.Caching.Memory.MemoryCacheOptions
{
    /// <summary>
    /// Gets or sets the name to be used for the cache instance.
    /// </summary>
    public string InstanceName { get; set; } = "default";

    /// <summary>
    /// 
    /// </summary>
    public bool IsBackplaneSource { get; set; } = false;

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
    /// </summary>
    /// <see cref="CacheUpdateMode"/>
    /// <value>None,Up</value>
    /// <remarks>Default: Up.</remarks>
    public CacheUpdateMode UpdateMode { get; set; } = CacheUpdateMode.Up;

    /// <summary>
    /// Gets or sets the cache key prefix.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the cache expire time.
    /// </summary>
    public TimeSpan? Expires { get; set; }
}
