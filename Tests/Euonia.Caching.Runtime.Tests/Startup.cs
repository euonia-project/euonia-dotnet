using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Caching.Runtime;

namespace Nerosoft.Euonia.Caching.Tests;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(builder =>
                   {
                       builder.AddJsonFile("appsettings.json");
                   })
                   .ConfigureServices((_, services) =>
                   {
                       services.AddOptions<RuntimeCacheOptions>()
                               .Configure(options =>
                               {
                                   // 使用非 "default" 的实例名，避免 RuntimeCacheHandle 要求通过配置文件
                                   // 配置 MemoryCache.Default 的限制，并保证测试之间的缓存实例相互独立。
                                   options.InstanceName = "runtime-tests";
                               });
                       services.AddSingleton<ICacheService, RuntimeCacheService>();
                   });
    }

    // ConfigureServices(IServiceCollection services)
    // ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    // ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
        
    }

    //public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
    //{
    //  InitData();
    //}

    public void Configure(IServiceProvider applicationServices)
    {
        //var config = applicationServices.GetService<IConfiguration>();
    }
}