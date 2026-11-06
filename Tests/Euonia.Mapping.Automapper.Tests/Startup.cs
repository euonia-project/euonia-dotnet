using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Mapping.Tests.Profiles;

namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 配置 AutoMapper 测试主机的启动引导类。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
	/// <summary>
	/// 配置主机的应用配置与服务容器，并加载 <c>appsettings.json</c> 配置文件。
	/// </summary>
	/// <param name="hostBuilder">用于构建测试主机的主机构建器。</param>
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
	/// 注册字符串辅助工具、值解析器、AutoMapper 配置与类型适配器工厂。
	/// </summary>
	/// <param name="services">依赖注入服务集合。</param>
	/// <param name="hostBuilderContext">主机构建上下文。</param>
	public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	{
		services.AddSingleton<StringHelper>();
		services.AddTransient(typeof(FullNameValueResolver<,>));
		services.Configure<AutomapperOptions>(options =>
		{
			options.AddProfile<Profile1>();
			options.AddProfile<Profile2>();
			options.AddProfile<Profile3>();
		});
		services.AddAutomapper();
		services.AddSingleton<ITypeAdapterFactory, AutomapperTypeAdapterFactory>();
	}

	//public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
	//{
	//}

	/// <summary>
	/// 将解析到的类型适配器工厂设置为全局当前实例。
	/// </summary>
	/// <param name="applicationServices">已构建的应用程序服务提供程序。</param>
	public void Configure(IServiceProvider applicationServices)
	{
		var factory = applicationServices.GetService<ITypeAdapterFactory>();
		if (factory != null)
		{
			TypeAdapterFactory.SetCurrent(factory);
		}
	}
}