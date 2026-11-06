using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块管理器。
/// </summary>
public class ModuleManager : IModuleManager, ISingletonDependency
{
    private readonly IModuleContainer _moduleContainer;
    private readonly IEnumerable<IModuleLifecycle> _lifecycleContributors;

    /// <summary>
    /// 初始化 <see cref="ModuleManager"/> 类的新实例。
    /// </summary>
    /// <param name="moduleContainer">模块容器，提供当前已加载的模块。</param>
    /// <param name="options">模块生命周期选项，指定参与生命周期的模块生命周期实现。</param>
    /// <param name="serviceProvider">用于解析生命周期参与者的服务提供程序。</param>
    public ModuleManager(IModuleContainer moduleContainer, IOptions<ModuleLifecycleOptions> options, IServiceProvider serviceProvider)
    {
        _moduleContainer = moduleContainer;

        _lifecycleContributors = options.Value
            .Lifecycle
            .Select(serviceProvider.GetRequiredService)
            .Cast<IModuleLifecycle>()
            .ToArray();
    }

    /// <summary>
    /// 初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文。</param>
    /// <exception cref="Exception">某个生命周期参与者初始化模块失败时抛出，内部异常为原始失败原因。</exception>
    public void InitializeModules(ApplicationInitializationContext context)
    {
        foreach (var contributor in _lifecycleContributors)
        {
            foreach (var module in _moduleContainer.Modules)
            {
                try
                {
                    contributor.Initialize(context, module.Instance);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format(Resources.IDS_MODULE_INITIALIZE_PHASE_FAILED, contributor.GetType().FullName, module.Type.AssemblyQualifiedName, ex.Message), ex);
                }
            }
        }
    }

    /// <summary>
    /// 关闭模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    /// <exception cref="Exception">某个生命周期参与者终止模块失败时抛出，内部异常为原始失败原因。</exception>
    public void UnloadModules(ApplicationShutdownContext context)
    {
        var modules = _moduleContainer.Modules.Reverse().ToList();

        foreach (var contributor in _lifecycleContributors)
        {
            foreach (var module in modules)
            {
                try
                {
                    contributor.Finalize(context, module.Instance);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format(Resources.IDS_MODULE_SHUTDOWN_PHASE_FAILED, contributor.GetType().FullName, module.Type.AssemblyQualifiedName, ex.Message), ex);
                }
            }
        }
    }
}
