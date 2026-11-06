using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 集成测试宿主的启动配置，负责加载配置文件并注册测试所需服务。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
	/// <summary>
	/// 配置测试宿主：加载 <c>appsettings.json</c> 并调用服务注册入口。
	/// </summary>
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
	/// 测试宿主启动时的服务注册入口，当前不需要注册额外服务。
	/// </summary>
	public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	{
	}

	//public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
	//{
	//  InitData();
	//}

	/// <summary>
	/// 测试宿主构建完成后的初始化入口，当前不需要执行初始化逻辑。
	/// </summary>
	public void Configure(IServiceProvider applicationServices)
	{
		//var config = applicationServices.GetService<IConfiguration>();
	}
}
