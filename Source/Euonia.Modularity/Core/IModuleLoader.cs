using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块加载器的契约。
/// </summary>
public interface IModuleLoader
{
    /// <summary>
    /// 从应用程序启动时加载模块。
    /// </summary>
    /// <param name="services">用于注册模块服务的服务集合。</param>
    /// <param name="startupModuleType">启动模块的类型，作为模块依赖图的根。</param>
    /// <returns>按依赖顺序加载的模块描述符数组。</returns>
    IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType);

    /// <summary>
    /// 加载启动模块依赖图以及额外来源提供的模块。
    /// </summary>
    /// <param name="services">用于注册模块服务的服务集合。</param>
    /// <param name="startupModuleType">启动模块的类型，作为模块依赖图的根。</param>
    /// <param name="additionalModuleTypes">额外模块类型，可为 <see langword="null"/>。</param>
    /// <returns>按依赖顺序加载的模块描述符数组。</returns>
    /// <remarks>未重写时忽略额外模块类型并回退到双参数重载。</remarks>
    IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType, IEnumerable<Type>? additionalModuleTypes)
    {
        return LoadModules(services, startupModuleType);
    }
}
