using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis;

internal class RedisSynchronizationModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.TryAddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connection = ConnectionMultiplexer.Connect(configuration.GetValue<string>("RedisSynchronization:ConnectionString"));
            var database = configuration.GetValue("RedisSynchronization:Database", -1);
            var expiry = configuration.GetValue<TimeSpan?>("RedisSynchronization:Expiry");
            var cadence = configuration.GetValue<TimeSpan?>("RedisSynchronization:ExtensionCadence");
            var validityTime = configuration.GetValue<TimeSpan?>("RedisSynchronization:MinValidityTime");
            var minTimeout = configuration.GetValue("RedisSynchronization:MinBusyWaitSleepTime", TimeSpan.FromMilliseconds(10));
            var maxTimeout = configuration.GetValue("RedisSynchronization:MaxBusyWaitSleepTime", TimeSpan.FromSeconds(0.8));

            return new RedisSynchronizationFactory(connection.GetDatabase(database), builder =>
            {
                Check.Ensure(expiry, value => value.HasValue).Success(value => builder.Expiry(value!.Value));
                Check.Ensure(cadence, value => value.HasValue).Success(value => builder.ExtensionCadence(value!.Value));
                Check.Ensure(validityTime, value => value.HasValue).Success(value => builder.MinValidityTime(value!.Value));
                builder.BusyWaitSleepTime(minTimeout, maxTimeout);
            });
        });
    }
}

/// <summary>
/// The <see cref="RedisLockModule"/> class contains methods used to configure the Redis lock.
/// </summary>
[DependsOn(typeof(RedisSynchronizationModule))]
public class RedisLockModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ILockFactory>(provider => provider.GetRequiredService<RedisSynchronizationFactory>());
    }
}

/// <summary>
/// The <see cref="RedisSemaphoreModule"/> class contains methods used to configure the Redis semaphore.
/// </summary>
[DependsOn(typeof(RedisSynchronizationModule))]
public class RedisSemaphoreModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISemaphoreFactory>(provider => provider.GetRequiredService<RedisSynchronizationFactory>());
    }
}