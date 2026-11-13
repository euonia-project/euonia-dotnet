using System.Collections.Specialized;
using System.Globalization;

namespace Nerosoft.Euonia.Caching.Runtime;

/// <summary>
/// <see cref="System.Runtime.Caching.MemoryCache"/> 的配置选项。
/// </summary>
public class RuntimeCacheOptions
{
    /// <summary>
    /// 获取或设置用于缓存实例的名称。
    /// </summary>
    public string InstanceName { get; set; } = "default";

    /// <summary>
    /// 获取或设置一个值，指示此缓存实例是否作为背板（backplane）的源。
    /// </summary>
    /// <remarks>当前实现未读取该选项。</remarks>
    public bool IsBackplaneSource { get; set; } = false;

    /// <summary>
    /// 获取或设置最大 Redis 访问重试次数。
    /// <remarks>默认值：5。</remarks>
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// 获取或设置缓存访问重试超时时间（毫秒）。
    /// <remarks>默认值：3000。</remarks>
    /// </summary>
    public int RetryTimeout { get; set; } = 3000;

    /// <summary>
    /// 获取或设置 Redis 缓存的更新模式。
    /// <see cref="CacheUpdateMode"/>
    /// </summary>
    /// <value>None、Up</value>
    /// <remarks>默认值：Up。</remarks>
    public CacheUpdateMode UpdateMode { get; set; } = CacheUpdateMode.Up;

    /// <summary>
    /// 获取或设置缓存键前缀。
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// 获取或设置缓存过期时间。
    /// </summary>
    public TimeSpan? Expires { get; set; }

    /// <summary>
    /// 一个整数值，指定 MemoryCache 实例可增长到的最大允许大小（以兆字节为单位）。默认值为 0，表示默认使用 MemoryCache 类的自动大小启发式算法。
    /// </summary>
    public int CacheMemoryLimitMegabytes { get; set; } = 0;

    /// <summary>
    /// 一个介于 0 和 100 之间的整数值，指定缓存可占用的物理安装内存的最大百分比。默认值为 0，表示默认使用 MemoryCache 类的自动大小启发式算法。
    /// </summary>
    public int PhysicalMemoryLimitPercentage { get; set; } = 0;

    /// <summary>
    /// 一个值，指示缓存实现将当前内存负载与为缓存实例设置的绝对内存限制和基于百分比的内存限制进行比较的时间间隔。
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 以 <see cref="NameValueCollection"/> 的形式获取配置
    /// </summary>
    /// <returns>包含当前配置的 <see cref="NameValueCollection"/>。</returns>
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
