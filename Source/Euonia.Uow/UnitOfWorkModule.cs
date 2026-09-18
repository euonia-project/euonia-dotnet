using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 负责将工作单元相关服务注册到依赖注入容器的模块。
/// </summary>
/// <remarks>
/// 该模块以瞬态（Transient）生命周期将 <see cref="UnitOfWorkInterceptor"/> 注册为 <see cref="IInterceptor"/>，
/// 以便代理机制对工作单元行为进行拦截；同时从配置节 <c>Euonia:Uow</c> 绑定 <see cref="UnitOfWorkOptions"/>。
/// </remarks>
public class UnitOfWorkModule : ModuleContextBase
{
	/// <summary>
	/// 在其它服务配置执行之前被调用，用于注册工作单元拦截器并绑定配置选项。
	/// </summary>
	/// <param name="context">提供当前 <see cref="IServiceCollection"/> 实例的 <see cref="ServiceConfigurationContext"/>。</param>
	/// <remarks>
	/// 拦截器以瞬态（Transient）生命周期注册，确保每次注入都获得新实例；
	/// 选项从配置节 <see cref="Constants.ConfigurationSection"/> 绑定。
	/// </remarks>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		// Register the UnitOfWorkInterceptor as a transient IInterceptor so each injection gets a new instance.
		context.Services.AddTransient<IInterceptor, UnitOfWorkInterceptor>();
		context.Services.Configure<UnitOfWorkOptions>(Configuration.GetSection("Euonia:Uow"));
	}

	/// <summary>
	/// 在服务配置阶段被调用，用于注册工作单元相关服务。
	/// </summary>
	/// <param name="context">提供当前 <see cref="IServiceCollection"/> 实例的 <see cref="ServiceConfigurationContext"/>。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddUnitOfWork();
	}
}