using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于配置内存服务总线组件的模块。
/// </summary>
public class InMemoryBusModule : ModuleContextBase
{
	/// <summary>
	/// 从应用程序配置中读取并配置内存总线选项。
	/// </summary>
	/// <param name="context">服务配置上下文。</param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.Configure<InMemoryBusOptions>(Configuration.GetSection(Constants.ConfigurationSection));
	}

	/// <summary>
	/// 配置内存服务总线所需的服务。
	/// </summary>
	/// <param name="context">
	/// 提供 <see cref="IServiceCollection"/> 和其他配置详情的 <see cref="ServiceConfigurationContext"/> 实例。
	/// </param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var name = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(InMemoryBusOptions.Name)}") ?? Constants.DefaultTransportName;
		context.Services.AddInMemoryBus(name);
	}
}