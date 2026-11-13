using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Caching.Memory;

namespace Nerosoft.Euonia.Caching.Tests;

/// <summary>
/// 内存缓存实现测试项目的宿主启动配置，负责注册内存缓存服务。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
    /// <summary>
    /// 配置测试宿主：加载 <c>appsettings.json</c>，并以独立实例名注册内存缓存服务。
    /// </summary>
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(builder =>
                   {
                       builder.AddJsonFile("appsettings.json");
                   })
                   .ConfigureServices((_, services) =>
                   {
                       services.AddOptions<MemoryCacheOptions>()
                               .Configure(options =>
                               {
                                   options.InstanceName = "memory-tests";
                               });
                       services.AddSingleton<ICacheService, MemoryCacheService>();
                   });
    }

    // ConfigureServices(IServiceCollection services)
    // ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    // ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    /// <summary>
    /// 测试宿主启动时的服务注册入口，当前不需要注册额外服务。
    /// </summary>
    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
        
    }

    //public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
    //{
    //}

    /// <summary>
    /// 测试宿主构建完成后的初始化入口，当前不需要执行初始化逻辑。
    /// </summary>
    public void Configure(IServiceProvider applicationServices)
    {
        //var config = applicationServices.GetService<IConfiguration>();
    }
}