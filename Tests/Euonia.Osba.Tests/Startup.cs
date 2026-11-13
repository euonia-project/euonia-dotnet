using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Nerosoft.Euonia.Osba;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// xunit 测试宿主的启动配置：加载配置文件并注册模块、业务对象与权限来源。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
	/// <summary>
	/// 配置主机：加载 <c>appsettings.json</c>，并注册模块、业务对象与权限来源。
	/// </summary>
	/// <param name="hostBuilder">测试宿主构建器。</param>
	public void ConfigureHost(IHostBuilder hostBuilder)
	{
		hostBuilder.ConfigureAppConfiguration(builder =>
		           {
			           builder.AddJsonFile("appsettings.json");
		           })
		           .ConfigureServices((_, services) =>
		           {
			           services.AddModularityApplication<BusinessTestModule>();
			           services.AddBusinessObject(typeof(Startup).Assembly);
			           services.AddPermission(permission =>
			           {
				           permission.Scan(typeof(Startup).Assembly);
				           permission.Source(ObjectPermissionCodeSource.Instance);
			           });
			           // 在此注册服务。
		           });
	}

	// ConfigureServices(IServiceCollection services)
	// ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	// ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
	/// <summary>
	/// 保留的服务配置入口：当前无需额外注册服务。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="hostBuilderContext">宿主构建上下文。</param>
	public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	{
	}

	//public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
	//{
	//  InitData();
	//}

	/// <summary>
	/// 保留的应用配置入口：当前不做任何初始化。
	/// </summary>
	/// <param name="applicationServices">应用级服务提供程序。</param>
	public void Configure(IServiceProvider applicationServices)
	{
		//var config = applicationServices.GetService<IConfiguration>();
	}
}