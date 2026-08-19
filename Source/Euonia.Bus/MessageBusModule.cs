using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息总线模块，负责注册消息总线的相关服务与配置。
/// </summary>
public class MessageBusModule : ModuleContextBase
{
	/// <summary>
	/// 在服务配置之前执行，注册并绑定 <see cref="MessageBusOptions"/> 选项。
	/// </summary>
	/// <param name="context">服务配置上下文。</param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddOptions<MessageBusOptions>()
		       .BindConfiguration(Constants.ConfigurationSection)
		       .Validate(_ => true);
	}

	/// <summary>
	/// 配置服务，向服务集合中添加消息总线。
	/// </summary>
	/// <param name="context">服务配置上下文。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		// 消息日志行为随总线模块注册（与 OutgoingLoggingBehavior 同属总线基础设施）。
		context.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MessageLoggingBehavior<,>));

		context.Services.AddEuoniaBus();		
	}

	/// <summary>
	/// 应用初始化完成后执行的回调方法。
	/// </summary>
	/// <param name="context">应用初始化上下文。</param>
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		// var configurator = context.ServiceProvider.GetService<IConfigurator>();
		// var builder = context.ServiceProvider.GetService<ConfiguratorBuilder>();
		// builder?.Invoke(configurator);
	}
}