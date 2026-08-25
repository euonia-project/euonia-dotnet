using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 定义应用服务的上下文契约，描述应用服务程序集与自动注册配置，并提供服务配置入口。
/// </summary>
public interface IServiceContext
{
	/// <summary>
	/// 获取应用服务所在程序集。
	/// </summary>
	Assembly Assembly { get; }

	/// <summary>
	/// 获取一个值，指示应用服务是否应自动注册。
	/// </summary>
	bool AutoRegisterApplicationService { get; }

	/// <summary>
	/// 获取一个值，指示管道行为是否应自动注册。
	/// </summary>
	bool AutoRegisterPipelineBehaviors { get; }

	/// <summary>
	/// 获取应用服务的生命周期。
	/// </summary>
	ServiceLifetime ApplicationServiceLifetime { get; }

	/// <summary>
	/// 配置应用所需的服务。
	/// </summary>
	/// <param name="services">用于注册应用服务的服务集合。</param>
	void ConfigureServices(IServiceCollection services);
}