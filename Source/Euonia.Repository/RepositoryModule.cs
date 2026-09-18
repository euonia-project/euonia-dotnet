using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 负责将仓储相关服务注册到依赖注入容器的模块。
/// </summary>
/// <remarks>
/// <see cref="ModuleContextBase"/> 的实现可重写 <see cref="ConfigureServices"/>
/// 以添加仓储功能所需的服务。本模块注册了仓储组件所依赖的上下文提供程序。
/// </remarks>
public class RepositoryModule : ModuleContextBase
{
	/// <summary>
	/// 在应用/服务配置阶段被调用，用于注册仓储服务。
	/// </summary>
	/// <param name="context">
	/// 提供当前 <see cref="IServiceCollection"/> 实例的 <see cref="ServiceConfigurationContext"/>，用于注册服务。
	/// </param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		// Registers the repository context provider into the DI container.
		context.Services.AddContextProvider();
	}
}