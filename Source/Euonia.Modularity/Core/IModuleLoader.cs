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
}
