using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 表示 ActiveMQ 总线模块，负责配置与 ActiveMQ 消息传递相关的服务。
/// </summary>
public class ActiveMqBusModule : ModuleContextBase
{
	/// <summary>
	/// 配置 ActiveMQ 消息传递所需的服务。
	/// </summary>
	/// <param name="context">服务配置上下文。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var enabled = Configuration.GetValue<bool>($"{Constants.ConfigurationSection}:{nameof(ActiveMqBusOptions.Enabled)}");
		var connection = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(ActiveMqBusOptions.Connection)}");
		var name = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(ActiveMqBusOptions.Name)}") ?? Constants.DefaultTransportName;

		// 从应用配置中配置 ActiveMQ 消息总线选项。
		context.Services.Configure<ActiveMqBusOptions>(Configuration.GetSection(Constants.ConfigurationSection));

		if (enabled && !string.IsNullOrWhiteSpace(connection))
		{
			context.Services.AddActiveMqBus(name);
		}
	}
}