using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 测试项目的启动配置，负责加载宿主配置并注册测试场景所需的服务。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
	/// <summary>
	/// 配置测试宿主：加载 <c>appsettings.json</c> 并调用服务注册回调。
	/// </summary>
	/// <param name="hostBuilder">待配置的宿主构建器。</param>
	public void ConfigureHost(IHostBuilder hostBuilder)
	{
		hostBuilder.ConfigureAppConfiguration(builder =>
		           {
			           builder.AddJsonFile("appsettings.json");
		           })
		           .ConfigureServices((_, _) =>
		           {
			           // 在此注册服务。
		           });
	}

	// ConfigureServices(IServiceCollection services)
    // ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    // ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    /// <summary>
    /// 注册测试宿主所需的服务；当前未注册任何服务，保留为测试场景的扩展入口。
    /// </summary>
    /// <param name="services">依赖注入容器中的服务集合。</param>
    /// <param name="hostBuilderContext">当前宿主构建上下文。</param>
    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
    }

    //public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
    //{
    //  InitData();
    //}

    /// <summary>
    /// 在宿主启动后执行测试数据或环境初始化；当前为空实现，保留为扩展入口。
    /// </summary>
    /// <param name="applicationServices">已构建的应用程序服务提供程序。</param>
    public void Configure(IServiceProvider applicationServices)
    {
        //var config = applicationServices.GetService<IConfiguration>();
    }
}
