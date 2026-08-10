using System.Reflection;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 用于根据配置实例化新的 <see cref="ICacheManager{TCacheValue}"/> 实例的辅助类。
/// </summary>
public static class CacheFactory
{
    /// <summary>
    /// <para>使用 <paramref name="settings"/> 定义的内联配置实例化缓存管理器。</para>
    /// <para>此 Build 方法返回缓存项类型为 <c>System.Object</c> 的 <c>ICacheManager</c>。</para>
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var cache = CacheFactory.Build("myCacheName", settings =>
    /// {
    ///    settings.WithUpdateMode(CacheUpdateMode.Up)
    ///            .WithDictionaryHandle()
    ///            .EnablePerformanceCounters()
    ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// cache.Add("key", "value");
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="cacheName">缓存管理器实例的名称。</param>
    /// <param name="settings">
    /// 配置。使用 settings 元素以流式方式配置缓存管理器实例、
    /// 添加缓存句柄并配置缓存句柄。
    /// </param>
    /// <returns>缓存项类型为 <c>System.Object</c> 的缓存管理器实例。</returns>
    /// <seealso cref="ICacheManager{TCacheValue}"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="cacheName"/> 或 <paramref name="settings"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static ICacheManager<object> Build(string cacheName, Action<ConfigurationBuilderCachePart> settings) =>
        Build<object>(cacheName, settings);

    /// <summary>
    /// <para>使用 <paramref name="settings"/> 定义的内联配置实例化缓存管理器。</para>
    /// <para>此 Build 方法返回缓存项类型为 <see cref="object"/> 的 <see cref="ICacheManager{TCacheValue}"/>。</para>
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var cache = CacheFactory.Build(settings =>
    /// {
    ///    settings.WithUpdateMode(CacheUpdateMode.Up)
    ///            .WithDictionaryHandle()
    ///            .EnablePerformanceCounters()
    ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// cache.Add("key", "value");
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="settings">
    /// 配置。使用 settings 元素以流式方式配置缓存管理器实例、
    /// 添加缓存句柄并配置缓存句柄。
    /// </param>
    /// <returns>缓存管理器实例。</returns>
    /// <seealso cref="ICacheManager{TCacheValue}"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="settings"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static ICacheManager<object> Build(Action<ConfigurationBuilderCachePart> settings) =>
        Build<object>(Guid.NewGuid().ToString("N"), settings);

    /// <summary>
    /// <para>使用 <paramref name="settings"/> 定义的内联配置实例化缓存管理器。</para>
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var cache = CacheFactory.Build("myCacheName", settings =>
    /// {
    ///    settings.WithUpdateMode(CacheUpdateMode.Up)
    ///            .WithDictionaryHandle()
    ///            .EnablePerformanceCounters()
    ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// cache.Add("key", "value");
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="cacheName">缓存管理器实例的名称。</param>
    /// <param name="settings">
    /// 配置。使用 settings 元素以流式方式配置缓存管理器实例、
    /// 添加缓存句柄并配置缓存句柄。
    /// </param>
    /// <typeparam name="TCacheValue">缓存项值的类型。</typeparam>
    /// <returns>缓存项类型为 <c>TCacheValue</c> 的缓存管理器实例。</returns>
    /// <seealso cref="ICacheManager{TCacheValue}"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="cacheName"/> 或 <paramref name="settings"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static ICacheManager<TCacheValue> Build<TCacheValue>(string cacheName, Action<ConfigurationBuilderCachePart> settings)
    {
        Check.EnsureNotNull(settings, nameof(settings));

        var part = new ConfigurationBuilderCachePart();
        settings(part);
        part.Configuration.Name = cacheName;
        return new BaseCacheManager<TCacheValue>(part.Configuration);
    }

    /// <summary>
    /// <para>使用 <paramref name="settings"/> 定义的内联配置实例化缓存管理器。</para>
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var cache = CacheFactory.Build(settings =>
    /// {
    ///    settings
    ///        .WithUpdateMode(CacheUpdateMode.Up)
    ///        .WithDictionaryHandle()
    ///            .EnablePerformanceCounters()
    ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// cache.Add("key", "value");
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="settings">
    /// 配置。使用 settings 元素以流式方式配置缓存管理器实例、
    /// 添加缓存句柄并配置缓存句柄。
    /// </param>
    /// <typeparam name="TCacheValue">缓存项值的类型。</typeparam>
    /// <returns>缓存项类型为 <c>TCacheValue</c> 的缓存管理器实例。</returns>
    /// <seealso cref="ICacheManager{TCacheValue}"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="settings"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static ICacheManager<TCacheValue> Build<TCacheValue>(Action<ConfigurationBuilderCachePart> settings)
        => Build<TCacheValue>(Guid.NewGuid().ToString("N"), settings);

    /// <summary>
    /// 使用给定的类型和 <paramref name="settings"/> 定义的内联配置实例化缓存管理器。
    /// 当无法调用泛型方法时使用此重载，例如与依赖注入结合使用时。
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var cache = CacheFactory.Build(typeof(string), "myCacheName", settings =>
    /// {
    ///    settings
    ///        .WithUpdateMode(CacheUpdateMode.Up)
    ///        .WithDictionaryHandle()
    ///            .EnablePerformanceCounters()
    ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="cacheValueType">缓存项值的类型。</param>
    /// <param name="cacheName">缓存管理器实例的名称。</param>
    /// <param name="settings">
    /// 配置。使用 settings 元素以流式方式配置缓存管理器实例、
    /// 添加缓存句柄并配置缓存句柄。
    /// </param>
    /// <returns>缓存管理器实例。</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="cacheName"/> 或 <paramref name="settings"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static object Build(Type cacheValueType, string cacheName, Action<ConfigurationBuilderCachePart> settings)
    {
        Check.EnsureNotNull(cacheValueType, nameof(cacheValueType));

        var factoryType = typeof(CacheFactory).GetTypeInfo();
        var buildMethod = factoryType.GetDeclaredMethods("Build").First(p => p.IsGenericMethod);
        var genericMethod = buildMethod.MakeGenericMethod(cacheValueType);
        return genericMethod.Invoke(null, [cacheName, settings]);
    }

    /// <summary>
    /// 使用给定的类型和 <paramref name="settings"/> 定义的内联配置实例化缓存管理器。
    /// 当无法调用泛型方法时使用此重载，例如与依赖注入结合使用时。
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var cache = CacheFactory.Build(typeof(string), settings =>
    /// {
    ///    settings.WithUpdateMode(CacheUpdateMode.Up)
    ///            .WithDictionaryHandle()
    ///            .EnablePerformanceCounters()
    ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="cacheValueType">缓存项值的类型。</param>
    /// <param name="settings">
    /// 配置。使用 settings 元素以流式方式配置缓存管理器实例、
    /// 添加缓存句柄并配置缓存句柄。
    /// </param>
    /// <returns>缓存管理器实例。</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="settings"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static object Build(Type cacheValueType, Action<ConfigurationBuilderCachePart> settings)
        => Build(cacheValueType, Guid.NewGuid().ToString("N"), settings);

    /// <summary>
    /// <para>使用给定的 <paramref name="configuration"/> 实例化缓存管理器。</para>
    /// </summary>
    /// <param name="cacheName">缓存的名称。</param>
    /// <param name="configuration">
    /// 将用于配置缓存管理器实例的配置。
    /// </param>
    /// <typeparam name="TCacheValue">缓存项值的类型。</typeparam>
    /// <returns>缓存管理器实例。</returns>
    /// <see cref="ConfigurationBuilder"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="configuration"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(string cacheName, CacheManagerConfiguration configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        var cfg = configuration;
        cfg.Name = cacheName;
        return new BaseCacheManager<TCacheValue>(cfg);
    }

    /// <summary>
    /// <para>使用给定的 <paramref name="configuration"/> 实例化缓存管理器。</para>
    /// </summary>
    /// <example>
    /// 以下示例展示了如何构建 <c>CacheManagerConfiguration</c>，然后
    /// 使用 <c>CacheFactory</c> 创建新的缓存管理器实例。
    /// <code>
    /// <![CDATA[
    /// var managerConfiguration = ConfigurationBuilder.BuildConfiguration<object>(settings =>
    /// {
    ///     settings.WithUpdateMode(CacheUpdateMode.Up)
    ///             .WithDictionaryCacheHandle<object>>()
    ///             .EnablePerformanceCounters()
    ///             .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
    /// });
    ///
    /// var cache = CacheFactory.FromConfiguration<object>(managerConfiguration);
    /// cache.Add("key", "value");
    /// ]]>
    /// </code>
    /// </example>
    /// <param name="configuration">
    /// 将用于配置缓存管理器实例的配置。
    /// </param>
    /// <typeparam name="TCacheValue">缓存项值的类型。</typeparam>
    /// <returns>缓存管理器实例。</returns>
    /// <see cref="ConfigurationBuilder"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="configuration"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(CacheManagerConfiguration configuration)
        => FromConfiguration<TCacheValue>(Guid.NewGuid().ToString("N"), configuration);

    /// <summary>
    /// 使用给定的 <paramref name="cacheValueType"/> 和 <paramref name="configuration"/> 实例化缓存管理器。
    /// 仅在无法使用泛型重载时使用此重载。返回类型将为 <c>Object</c>。
    /// 此方法可用于例如与依赖注入框架结合使用的场景。
    /// </summary>
    /// <param name="cacheValueType">缓存项值的类型。</param>
    /// <param name="cacheName">缓存的名称。</param>
    /// <param name="configuration">
    /// 将用于配置缓存管理器实例的配置。
    /// </param>
    /// <returns>缓存管理器实例。</returns>
    /// <see cref="ConfigurationBuilder"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <c>cacheValueType</c>、<c>cacheName</c> 或 <c>configuration</c> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static object FromConfiguration(Type cacheValueType, string cacheName, CacheManagerConfiguration configuration)
    {
        Check.EnsureNotNull(cacheValueType, nameof(cacheValueType));
        Check.EnsureNotNull(configuration, nameof(configuration));

        var cfg = configuration;
        cfg.Name = cacheName;

        var type = typeof(BaseCacheManager<>).MakeGenericType(cacheValueType);
        return Activator.CreateInstance(type, cfg);
    }

    /// <summary>
    /// 使用给定的 <paramref name="cacheValueType"/> 和 <paramref name="configuration"/> 实例化缓存管理器。
    /// 仅在无法使用泛型重载时使用此重载。返回类型将为 <c>Object</c>。
    /// 此方法可用于例如与依赖注入框架结合使用的场景。
    /// </summary>
    /// <param name="cacheValueType">缓存项值的类型。</param>
    /// <param name="configuration">
    /// 将用于配置缓存管理器实例的配置。
    /// </param>
    /// <returns>缓存管理器实例。</returns>
    /// <see cref="ConfigurationBuilder"/>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="cacheValueType"/> 或 <paramref name="configuration"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// 当缓存句柄相关的某些配置错误发生时抛出。
    /// </exception>
    public static object FromConfiguration(Type cacheValueType, CacheManagerConfiguration configuration)
        => FromConfiguration(cacheValueType, Guid.NewGuid().ToString("N"), configuration);
}
