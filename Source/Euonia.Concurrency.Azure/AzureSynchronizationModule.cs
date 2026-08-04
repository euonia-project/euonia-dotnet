using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// Azure 同步的模块标记，作为 <see cref="AzureLockModule"/> 的依赖项。
/// </summary>
internal class AzureSynchronizationModule : ModuleContextBase
{
}

/// <summary>
/// 用于配置 Azure 锁的模块。
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