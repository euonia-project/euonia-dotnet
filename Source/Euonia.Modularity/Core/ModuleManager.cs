using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块管理器。
/// </summary>
public class ModuleManager : IModuleManager, ISingletonDependency
{
    private readonly IModuleContainer _moduleContainer;
    private readonly Type[] _lifecycleTypes;

    private int _initialized;

    /// <summary>
    /// 初始化 <see cref="ModuleManager"/> 类的新实例。
    /// </summary>
    /// <param name="moduleContainer">模块容器，提供当前已加载的模块。</param>
    /// <param name="options">模块生命周期选项，指定参与生命周期的模块生命周期实现。</param>
    public ModuleManager(IModuleContainer moduleContainer, IOptions<ModuleLifecycleOptions> options)
    {
        _moduleContainer = Check.EnsureNotNull(moduleContainer, nameof(moduleContainer));
        _lifecycleTypes = Check.EnsureNotNull(options, nameof(options)).Value.Lifecycle.ToArray();
    }

    /// <summary>
    /// 获取一个值，指示模块是否已经初始化。
    /// </summary>
    public bool IsInitialized => Volatile.Read(ref _initialized) != 0;

    /// <summary>
    /// 初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文。</param>
    /// <exception cref="Exception">某个生命周期参与者初始化模块失败时抛出，内部异常为原始失败原因。</exception>
    /// <remarks>
    /// 该方法可安全地重复调用：首个调用完成模块初始化，后续调用将被忽略。
    /// 生命周期参与者在当前初始化作用域中解析，以支持作用域级依赖。
    /// </remarks>
    public void InitializeModules(ApplicationInitializationContext context)
    {
        Check.EnsureNotNull(context, nameof(context));

        if (IsInitialized)
        {
            return;
        }

        var lifecycleContributors = ResolveLifecycleContributors(context.ServiceProvider);

        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
        {
            return;
        }

        foreach (var contributor in lifecycleContributors)
        {
            foreach (var module in _moduleContainer.Modules)
            {
                try
                {
                    contributor.Initialize(context, module.Instance);
                }
                catch (Exception exception)
                {
                    throw CreateInitializeException(contributor, module, exception);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task InitializeModulesAsync(ApplicationInitializationContext context, CancellationToken cancellationToken = default)
    {
        Check.EnsureNotNull(context, nameof(context));

        if (IsInitialized)
        {
            return;
        }

        var lifecycleContributors = ResolveLifecycleContributors(context.ServiceProvider);

        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
        {
            return;
        }

        foreach (var contributor in lifecycleContributors)
        {
            foreach (var module in _moduleContainer.Modules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await contributor.InitializeAsync(context, module.Instance).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw CreateInitializeException(contributor, module, exception);
                }
            }
        }
    }

    /// <summary>
    /// 关闭模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    /// <exception cref="Exception">某个生命周期参与者终止模块失败时抛出，内部异常为原始失败原因。</exception>
    /// <remarks>
    /// 该方法可安全地重复调用：首个调用按初始化顺序的逆序终止模块，后续调用将被忽略。
    /// </remarks>
    public void UnloadModules(ApplicationShutdownContext context)
    {
        Check.EnsureNotNull(context, nameof(context));

        if (!IsInitialized)
        {
            return;
        }

        var lifecycleContributors = ResolveLifecycleContributors(context.ServiceProvider);

        if (Interlocked.Exchange(ref _initialized, 0) == 0)
        {
            return;
        }

        var modules = _moduleContainer.Modules.Reverse().ToArray();

        foreach (var contributor in lifecycleContributors)
        {
            foreach (var module in modules)
            {
                try
                {
                    contributor.Finalize(context, module.Instance);
                }
                catch (Exception exception)
                {
                    throw CreateShutdownException(contributor, module, exception);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task UnloadModulesAsync(ApplicationShutdownContext context, CancellationToken cancellationToken = default)
    {
        Check.EnsureNotNull(context, nameof(context));

        if (!IsInitialized)
        {
            return;
        }

        var lifecycleContributors = ResolveLifecycleContributors(context.ServiceProvider);

        if (Interlocked.Exchange(ref _initialized, 0) == 0)
        {
            return;
        }

        var modules = _moduleContainer.Modules.Reverse().ToArray();

        foreach (var contributor in lifecycleContributors)
        {
            foreach (var module in modules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await contributor.FinalizeAsync(context, module.Instance).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw CreateShutdownException(contributor, module, exception);
                }
            }
        }
    }

    private static InvalidOperationException CreateInitializeException(IModuleLifecycle contributor, IModuleDescriptor module, Exception exception)
    {
        return new InvalidOperationException(
            string.Format(Resources.IDS_MODULE_INITIALIZE_PHASE_FAILED, contributor.GetType().FullName, module.Type.AssemblyQualifiedName, exception.Message),
            exception);
    }

    private static InvalidOperationException CreateShutdownException(IModuleLifecycle contributor, IModuleDescriptor module, Exception exception)
    {
        return new InvalidOperationException(
            string.Format(Resources.IDS_MODULE_SHUTDOWN_PHASE_FAILED, contributor.GetType().FullName, module.Type.AssemblyQualifiedName, exception.Message),
            exception);
    }

    private IModuleLifecycle[] ResolveLifecycleContributors(IServiceProvider serviceProvider)
    {
        Check.EnsureNotNull(serviceProvider, nameof(serviceProvider));

        return _lifecycleTypes
            .Select(type => (IModuleLifecycle)serviceProvider.GetRequiredService(type))
            .ToArray();
    }
}
