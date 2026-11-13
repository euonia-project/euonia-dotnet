namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// 内存缓存选项。
/// </summary>
public class MemoryCacheOptions : Microsoft.Extensions.Caching.Memory.MemoryCacheOptions
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
    /// </summary>
    /// <see cref="CacheUpdateMode"/>
    /// <value>None,Up</value>
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
}
