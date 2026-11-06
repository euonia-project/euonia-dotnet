using Nerosoft.Euonia.Caching.Runtime;
using System.Runtime.Caching;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Extensions for the configuration builder specific to System.Runtime.Caching cache handle.
/// </summary>
public static class RuntimeCacheBuilderExtensions
{

    private const string DEFAULT_NAME = "default";

    /// <summary>
    /// Adds a <see cref="RuntimeCacheHandle{TValue}" /> using a <see cref="MemoryCache"/>.
    /// The name of the cache instance will be 'default'.
    /// </summary>
    /// <param name="part">The builder part.</param>
    /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
    /// This setting will be ignored if no backplane is configured.</param>
    /// <returns>
    /// The builder part.
    /// </returns>
    /// <returns>The builder part.</returns>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, bool isBackplaneSource = false)
        => part?.WithHandle(typeof(RuntimeCacheHandle<>), DEFAULT_NAME, isBackplaneSource);

    /// <summary>
    /// Adds a <see cref="RuntimeCacheHandle{TValue}" /> using a <see cref="MemoryCache"/> instance with the given <paramref name="instanceName"/>.
    /// The named cache instance can be configured via <c>app/web.config</c> <c>system.runtime.caching</c> section.
    /// </summary>
    /// <param name="part">The builder part.</param>
    /// <param name="instanceName">The name to be used for the cache instance.</param>
    /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
    /// This setting will be ignored if no backplane is configured.</param>
    /// <returns>
    /// The builder part.
    /// </returns>
    /// <exception cref="ArgumentNullException">If part is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource = false)
        => part?.WithHandle(typeof(RuntimeCacheHandle<>), instanceName, isBackplaneSource);

    /// <summary>
    /// Adds a <see cref="RuntimeCacheHandle{TValue}" /> using a <see cref="MemoryCache"/> instance with the given <paramref name="instanceName"/>.
    /// The named cache instance can be configured via <paramref name="options"/>.
    /// </summary>
    /// <param name="part">The builder part.</param>
    /// <param name="instanceName">The name to be used for the cache instance.</param>
    /// <param name="options">
    /// The <see cref="RuntimeCacheOptions"/> which should be used to initiate this cache.
    /// If <c>Null</c>, default options will be used.
    /// </param>
    /// <returns>
    /// The builder part.
    /// </returns>
    /// <exception cref="ArgumentNullException">If part is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, RuntimeCacheOptions options)
        => WithRuntimeCacheHandle(part, instanceName, false, options);

    /// <summary>
    /// Adds a <see cref="RuntimeCacheHandle{TValue}" /> using a <see cref="MemoryCache"/> instance with the given <paramref name="instanceName"/>.
    /// The named cache instance can be configured via <paramref name="options"/>.
    /// </summary>
    /// <param name="part">The builder part.</param>
    /// <param name="instanceName">The name to be used for the cache instance.</param>
    /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
    /// This setting will be ignored if no backplane is configured.</param>
    /// <param name="options">
    /// The <see cref="RuntimeCacheOptions"/> which should be used to initiate this cache.
    /// If <c>Null</c>, default options will be used.
    /// </param>
    /// <returns>
    /// The builder part.
    /// </returns>
    /// <exception cref="ArgumentNullException">If part is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
    public static ConfigurationBuilderCacheHandlePart? WithRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource, RuntimeCacheOptions options)
        => part?.WithHandle(typeof(RuntimeCacheHandle<>), instanceName, isBackplaneSource, options);
}
