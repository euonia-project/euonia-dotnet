using System.Collections.Specialized;
using System.Globalization;

namespace Nerosoft.Euonia.Caching.Runtime;

/// <summary>
/// <see cref="System.Runtime.Caching.MemoryCache"/> configuration options
/// </summary>
public class RuntimeCacheOptions
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
    /// <see cref="CacheUpdateMode"/>
    /// </summary>
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

    /// <summary>
    /// An integer value that specifies the maximum allowable size, in megabytes, that an instance of a MemoryCache can grow to. The default value is 0, which means that the autosizing heuristics of the MemoryCache class are used by default.
    /// </summary>
    public int CacheMemoryLimitMegabytes { get; set; } = 0;

    /// <summary>
    /// An integer value between 0 and 100 that specifies the maximum percentage of physically installed computer memory that can be consumed by the cache. The default value is 0, which means that the autosizing heuristics of the MemoryCache class are used by default.
    /// </summary>
    public int PhysicalMemoryLimitPercentage { get; set; } = 0;

    /// <summary>
    /// A value that indicates the time interval after which the cache implementation compares the current memory load against the absolute and percentage-based memory limits that are set for the cache instance.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets the configuration as a <see cref="NameValueCollection"/>
    /// </summary>
    /// <returns>A <see cref="NameValueCollection"/> with the current configuration.</returns>
    public NameValueCollection AsNameValueCollection()
    {
        return new NameValueCollection(3)
        {
            { nameof(CacheMemoryLimitMegabytes), CacheMemoryLimitMegabytes.ToString(CultureInfo.InvariantCulture) },
            { nameof(PhysicalMemoryLimitPercentage), PhysicalMemoryLimitPercentage.ToString(CultureInfo.InvariantCulture) },
            { nameof(PollingInterval), PollingInterval.ToString("c") }
        };
    }
}
