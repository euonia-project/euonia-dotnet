using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 表示 RabbitMQ 总线模块，负责配置与 RabbitMQ 消息传递相关的服务。
/// </summary>
public class RabbitMqBusModule : ModuleContextBase
{
	/// <summary>
	/// 配置 RabbitMQ 消息传递所需的服务。
	/// </summary>
	/// <param name="context">服务配置上下文。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var enabled = Configuration.GetValue<bool>($"{Constants.ConfigurationSection}:{nameof(RabbitMqBusOptions.Enabled)}");
		var connection = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(RabbitMqBusOptions.Connection)}");
		var name = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(RabbitMqBusOptions.Name)}") ?? Constants.DefaultTransportName;

		// 从应用程序配置中读取 RabbitMQ 消息总线选项。
		context.Services.Configure<RabbitMqBusOptions>(Configuration.GetSection(Constants.ConfigurationSection));

		if (enabled && !string.IsNullOrWhiteSpace(connection))
		{
			context.Services.AddRabbitMqBus(name);
		}
	}
}