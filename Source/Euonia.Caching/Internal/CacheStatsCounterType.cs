namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 定义缓存管理器支持的不同计数器类型。
/// </summary>
public enum CacheStatsCounterType
{
    /// <summary>
    /// 命中次数。
    /// </summary>
    Hits,

    /// <summary>
    /// 未命中次数。
    /// </summary>
    Misses,

    /// <summary>
    /// 项的总数。
    /// <para>
    /// 在分布式缓存场景中这可能不准确，因为只统计本地添加或移除的项。
    /// </para>
    /// </summary>
    Items,

    /// <summary>
    /// 移除调用的次数。
    /// </summary>
    RemoveCalls,

    /// <summary>
    /// 添加调用的次数。
    /// </summary>
    AddCalls,

    /// <summary>
    /// 更新调用的次数。
    /// </summary>
    PutCalls,

    /// <summary>
    /// 获取调用的次数。
    /// </summary>
    GetCalls,

    /// <summary>
    /// 清空调用的次数。
    /// </summary>
    ClearCalls,

    /// <summary>
    /// 清空区域调用的次数。
    /// </summary>
    ClearRegionCalls
}