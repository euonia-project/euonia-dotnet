using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// The module manager.
/// </summary>
public class ModuleManager : IModuleManager, ISingletonDependency
{
    private readonly IModuleContainer _moduleContainer;
    private readonly IEnumerable<IModuleLifecycle> _lifecycleContributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleManager"/> class.
    /// </summary>
    /// <param name="moduleContainer"></param>
    /// <param name="options"></param>
    /// <param name="serviceProvider"></param>
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
    /// Initializes the modules.
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="Exception"></exception>
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
    /// Shutdowns the modules.
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="Exception"></exception>
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
