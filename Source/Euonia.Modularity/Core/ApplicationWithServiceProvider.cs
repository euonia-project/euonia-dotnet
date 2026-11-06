using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// A base class for applications that provides access to the <see cref="IServiceProvider"/>.
/// </summary>
public class ApplicationWithServiceProvider : ModularityApplicationBase, IApplicationWithServiceProvider
{
    /// <inheritdoc />
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
        SetServiceProvider(serviceProvider);
        InitializeModules();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();

        if (ServiceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}