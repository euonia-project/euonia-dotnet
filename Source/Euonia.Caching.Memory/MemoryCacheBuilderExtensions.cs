using Microsoft.Extensions.Caching.Memory;
using Nerosoft.Euonia.Caching.Memory;
using MemoryCacheOptions = Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 适用于基于 <see cref="Microsoft.Extensions.Caching.Memory"/> 的缓存的配置生成器扩展。
/// </summary>
public static class MemoryCacheBuilderExtensions
{
    /// <summary>
    /// 使用 <see cref="MemoryCache"/> 添加缓存句柄。
    /// </summary>
    /// <param name="part">生成器部件。</param>
    /// <param name="isBackplaneSource">如果此缓存句柄应作为背板的数据源，请将其设置为 true。
    /// 未配置背板时，此设置将被忽略。</param>
    /// <returns>
    /// 生成器部件。
    /// </returns>
    /// <returns>生成器部件。</returns>
    public static ConfigurationBuilderCacheHandlePart? WithMemoryCacheHandle(this ConfigurationBuilderCachePart part, bool isBackplaneSource = false)
        => WithMemoryCacheHandle(part, Guid.NewGuid().ToString(), isBackplaneSource, new MemoryCacheOptions());

    /// <summary>
    /// 使用 <see cref="MemoryCache"/> 添加缓存句柄。
    /// </summary>
    /// <param name="part">生成器部件。</param>
    /// <param name="instanceName">用于缓存句柄实例的名称。</param>
    /// <param name="isBackplaneSource">如果此缓存句柄应作为背板的数据源，请将其设置为 true。
    /// 未配置背板时，此设置将被忽略。</param>
    /// <returns>
    /// 生成器部件。
    /// </returns>
    /// <exception cref="ArgumentNullException">如果 part 为 null。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="instanceName"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCacheHandlePart? WithMemoryCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource = false)
        => WithMemoryCacheHandle(part, instanceName, isBackplaneSource, new MemoryCacheOptions());

    /// <summary>
    /// 使用 <see cref="MemoryCache"/> 添加缓存句柄。
    /// </summary>
    /// <param name="part">生成器部件。</param>
    /// <param name="instanceName">用于缓存句柄实例的名称。</param>
    /// <param name="options">用于初始化或重置此缓存的 <see cref="MemoryCacheOptions"/>。</param>
    /// <returns>生成器部件。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="instanceName"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCacheHandlePart? WithMemoryCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, MemoryCacheOptions options)
        => WithMemoryCacheHandle(part, instanceName, false, options);

    /// <summary>
    /// 使用 <see cref="MemoryCache"/> 添加缓存句柄。
    /// 缓存实例的名称将为 'default'。
    /// </summary>
    /// <param name="part">生成器部件。</param>
    /// <param name="options">用于初始化或重置此缓存的 <see cref="MemoryCacheOptions"/>。</param>
    /// <returns>生成器部件。</returns>
    public static ConfigurationBuilderCacheHandlePart? WithMemoryCacheHandle(this ConfigurationBuilderCachePart part, MemoryCacheOptions options)
        => WithMemoryCacheHandle(part, Guid.NewGuid().ToString(), false, options);

    /// <summary>
    /// 使用 <see cref="MemoryCache"/> 添加缓存句柄。
    /// </summary>
    /// <param name="part">生成器部件。</param>
    /// <param name="instanceName">用于缓存句柄实例的名称。</param>
    /// <param name="isBackplaneSource">如果此缓存句柄应作为背板的数据源，请将其设置为 true。
    /// 未配置背板时，此设置将被忽略。</param>
    /// <param name="options">
    /// 用于初始化或重置此缓存的 <see cref="MemoryCacheOptions"/>。
    /// 如果为 <c>Null</c>，将使用默认选项。
    /// </param>
    /// <returns>生成器部件。</returns>
    /// <exception cref="ArgumentNullException">如果 part 为 null。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="instanceName"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCacheHandlePart? WithMemoryCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource, MemoryCacheOptions options)
        => part?.WithHandle(typeof(MemoryCacheHandle<>), instanceName, isBackplaneSource, options);
}