using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.Azure;

internal class AzureSynchronizationModule : ModuleContextBase
{
}

/// <summary>
/// The <see cref="AzureLockModule"/> class contains methods used to configure the Azure lock.
/// </summary>
[DependsOn(typeof(AzureSynchronizationModule))]
public class AzureLockModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ILockFactory, AzureSynchronizationFactory>();
    }
}