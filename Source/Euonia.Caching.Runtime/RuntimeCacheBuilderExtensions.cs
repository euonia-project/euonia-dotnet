using Nerosoft.Euonia.Caching.Runtime;
using System.Runtime.Caching;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 针对 System.Runtime.Caching 缓存句柄的配置生成器扩展。
/// </summary>
public static class RuntimeCacheBuilderExtensions
{

    private const string DEFAULT_NAME = "default";

    /// <summary>
    /// 使用 <see cref="MemoryCache"/> 添加一个 <see cref="RuntimeCacheHandle{TValue}" />。
    /// 缓存实例的名称将为 'default'。
    /// </summary>
    /// <param name="part">生成器部分。</param>
    /// <param name="isBackplaneSource">如果此缓存句柄应作为背板的来源，请将此值设置为 true。
    /// 如果未配置背板，则忽略此设置。</param>
    /// <returns>
    /// 生成器部分。
    /// </returns>
    /// <returns>生成器部分。</returns>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, bool isBackplaneSource = false)
        => part?.WithHandle(typeof(RuntimeCacheHandle<>), DEFAULT_NAME, isBackplaneSource);

    /// <summary>
    /// 使用具有指定 <paramref name="instanceName"/> 的 <see cref="MemoryCache"/> 实例添加一个 <see cref="RuntimeCacheHandle{TValue}" />。
    /// 可通过 <c>app/web.config</c> 的 <c>system.runtime.caching</c> 节配置命名缓存实例。
    /// </summary>
    /// <param name="part">生成器部分。</param>
    /// <param name="instanceName">用于缓存实例的名称。</param>
    /// <param name="isBackplaneSource">如果此缓存句柄应作为背板的来源，请将此值设置为 true。
    /// 如果未配置背板，则忽略此设置。</param>
    /// <returns>
    /// 生成器部分。
    /// </returns>
    /// <exception cref="ArgumentNullException">如果 part 为 null。</exception>
    /// <exception cref="ArgumentNullException">如果 <paramref name="instanceName"/> 为 null，则抛出此异常。</exception>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource = false)
        => part?.WithHandle(typeof(RuntimeCacheHandle<>), instanceName, isBackplaneSource);

    /// <summary>
    /// 使用具有指定 <paramref name="instanceName"/> 的 <see cref="MemoryCache"/> 实例添加一个 <see cref="RuntimeCacheHandle{TValue}" />。
    /// 可通过 <paramref name="options"/> 配置命名缓存实例。
    /// </summary>
    /// <param name="part">生成器部分。</param>
    /// <param name="instanceName">用于缓存实例的名称。</param>
    /// <param name="options">
    /// 用于初始化此缓存的 <see cref="RuntimeCacheOptions"/>。
    /// 如果为 <c>Null</c>，将使用默认选项。
    /// </param>
    /// <returns>
    /// 生成器部分。
    /// </returns>
    /// <exception cref="ArgumentNullException">如果 part 为 null。</exception>
    /// <exception cref="ArgumentNullException">如果 <paramref name="instanceName"/> 为 null，则抛出此异常。</exception>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, RuntimeCacheOptions options)
        => WithRuntimeCacheHandle(part, instanceName, false, options);

    /// <summary>
    /// 使用具有指定 <paramref name="instanceName"/> 的 <see cref="MemoryCache"/> 实例添加一个 <see cref="RuntimeCacheHandle{TValue}" />。
    /// 可通过 <paramref name="options"/> 配置命名缓存实例。
    /// </summary>
    /// <param name="part">生成器部分。</param>
    /// <param name="instanceName">用于缓存实例的名称。</param>
    /// <param name="isBackplaneSource">如果此缓存句柄应作为背板的来源，请将此值设置为 true。
    /// 如果未配置背板，则忽略此设置。</param>
    /// <param name="options">
    /// 用于初始化此缓存的 <see cref="RuntimeCacheOptions"/>。
    /// 如果为 <c>Null</c>，将使用默认选项。
    /// </param>
    /// <returns>
    /// 生成器部分。
    /// </returns>
    /// <exception cref="ArgumentNullException">如果 part 为 null。</exception>
    /// <exception cref="ArgumentNullException">如果 <paramref name="instanceName"/> 为 null，则抛出此异常。</exception>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource, RuntimeCacheOptions options)
        => part?.WithHandle(typeof(RuntimeCacheHandle<>), instanceName, isBackplaneSource, options);
}
