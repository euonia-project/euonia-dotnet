using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 为应用程序提供 <see cref="IServiceProvider"/> 访问的基类。
/// </summary>
public class ApplicationWithServiceProvider : ModularityApplicationBase, IApplicationWithServiceProvider
{
    private int _disposed;

    /// <summary>
    /// 初始化 <see cref="ApplicationWithServiceProvider"/> 类的新实例。
    /// </summary>
    /// <param name="startupModuleType">启动模块类型，作为模块依赖图的根节点。</param>
    /// <param name="services">应用程序的服务集合。</param>
    /// <param name="configuration">应用程序配置；为 <c>null</c> 时按创建选项构建配置。</param>
    /// <param name="optionsAction">用于自定义应用程序创建选项的委托，可为 <c>null</c>。</param>
    public ApplicationWithServiceProvider(Type startupModuleType, IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction)
        : base(startupModuleType, services, configuration, optionsAction)
    {
        services.AddSingleton<IApplicationWithServiceProvider>(this);
    }

    void IApplicationWithServiceProvider.SetServiceProvider(IServiceProvider serviceProvider)
    {
        SetServiceProvider(serviceProvider);
    }

    /// <inheritdoc />
    public void Initialize(IServiceProvider serviceProvider)
    {
        Check.EnsureNotNull(serviceProvider, nameof(serviceProvider));

        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(GetType().FullName, Resources.IDS_APPLICATION_ALREADY_DISPOSED);
        }

        if (IsShutdown)
        {
            throw new InvalidOperationException(Resources.IDS_APPLICATION_ALREADY_SHUTDOWN);
        }

        SetServiceProvider(serviceProvider);
        InitializeModules();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            Shutdown();
        }
        finally
        {
            try
            {
                if (ServiceProviderOrNull is IDisposable disposableServiceProvider)
                {
                    disposableServiceProvider.Dispose();
                }
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
