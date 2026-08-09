using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

internal class ZooKeeperSynchronizationModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.TryAddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connection = configuration.GetValue<string>("ZooKeeperSynchronization:ConnectionString");
            var sessionTimeout = configuration.GetValue("ZooKeeperSynchronization:SessionTimeout", 0);
            var connectTimeout = configuration.GetValue("ZooKeeperSynchronization:ConnectTimeout", 0);

            return new ZooKeeperSynchronizationFactory(connection, builder =>
            {
                Check.Ensure(sessionTimeout, value => value > 0).Success(value => builder.SessionTimeout(TimeSpan.FromMilliseconds(value)));
                Check.Ensure(connectTimeout, value => value > 0).Success(value => builder.ConnectTimeout(TimeSpan.FromMilliseconds(value)));
            });
        });
    }
}

/// <summary>
/// The ZooKeeperLockModule class contains methods used to configure the ZooKeeper lock.
/// </summary>
[DependsOn(typeof(ZooKeeperSynchronizationModule))]
public class ZooKeeperLockModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ILockFactory>(provider => provider.GetRequiredService<ZooKeeperSynchronizationFactory>());
    }
}

/// <summary>
/// The ZooKeeperSemaphoreModule class contains methods used to configure the ZooKeeper semaphore.
/// </summary>
[DependsOn(typeof(ZooKeeperSynchronizationModule))]
public class ZooKeeperSemaphoreModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISemaphoreFactory>(provider => provider.GetRequiredService<ZooKeeperSynchronizationFactory>());
    }
}