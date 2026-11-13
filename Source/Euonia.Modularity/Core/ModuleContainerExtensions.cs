namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 为 <see cref="IModuleContainer"/> 提供模块查找扩展方法。
/// </summary>
public static class ModuleContainerExtensions
{
    /// <summary>
    /// 查找指定类型的模块描述符。
    /// </summary>
    /// <param name="container">模块容器。</param>
    /// <param name="moduleType">模块类型。</param>
    /// <returns>匹配的模块描述符；未找到时为 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="container"/> 或 <paramref name="moduleType"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IModuleDescriptor? FindModule(this IModuleContainer container, Type moduleType)
    {
        Check.EnsureNotNull(container, nameof(container));
        Check.EnsureNotNull(moduleType, nameof(moduleType));

        return container.Modules.FirstOrDefault(module => module.Type == moduleType);
    }

    /// <summary>
    /// 查找指定类型的模块描述符。
    /// </summary>
    /// <typeparam name="TModule">模块类型。</typeparam>
    /// <param name="container">模块容器。</param>
    /// <returns>匹配的模块描述符；未找到时为 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="container"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IModuleDescriptor? FindModule<TModule>(this IModuleContainer container)
        where TModule : IModuleContext
    {
        return container.FindModule(typeof(TModule));
    }

    /// <summary>
    /// 获取指定类型的模块描述符。
    /// </summary>
    /// <param name="container">模块容器。</param>
    /// <param name="moduleType">模块类型。</param>
    /// <returns>匹配的模块描述符。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="container"/> 或 <paramref name="moduleType"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">指定类型的模块未加载时抛出。</exception>
    public static IModuleDescriptor GetModule(this IModuleContainer container, Type moduleType)
    {
        return container.FindModule(moduleType)
               ?? throw new InvalidOperationException(string.Format(Resources.IDS_MODULE_NOT_FOUND, moduleType.AssemblyQualifiedName));
    }

    /// <summary>
    /// 获取指定类型的模块描述符。
    /// </summary>
    /// <typeparam name="TModule">模块类型。</typeparam>
    /// <param name="container">模块容器。</param>
    /// <returns>匹配的模块描述符。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="container"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">指定类型的模块未加载时抛出。</exception>
    public static IModuleDescriptor GetModule<TModule>(this IModuleContainer container)
        where TModule : IModuleContext
    {
        return container.GetModule(typeof(TModule));
    }

    /// <summary>
    /// 尝试获取指定类型的模块描述符。
    /// </summary>
    /// <typeparam name="TModule">模块类型。</typeparam>
    /// <param name="container">模块容器。</param>
    /// <param name="module">匹配的模块描述符；未找到时为 <see langword="null"/>。</param>
    /// <returns>找到模块时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    public static bool TryGetModule<TModule>(this IModuleContainer container, out IModuleDescriptor? module)
        where TModule : IModuleContext
    {
        module = container.FindModule<TModule>();
        return module != null;
    }

    /// <summary>
    /// 判断指定类型的模块是否已加载。
    /// </summary>
    /// <typeparam name="TModule">模块类型。</typeparam>
    /// <param name="container">模块容器。</param>
    /// <returns>模块已加载时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    public static bool IsModuleLoaded<TModule>(this IModuleContainer container)
        where TModule : IModuleContext
    {
        return container.FindModule<TModule>() != null;
    }
}
