using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// Represents the ActiveMQ Bus Module, responsible for configuring services related to ActiveMQ messaging.
/// </summary>
public class ActiveMqBusModule : ModuleContextBase
{
	/// <summary>
	/// Configures the services required for ActiveMQ messaging.
	/// </summary>
	/// <param name="context">The service configuration context.</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var enabled = Configuration.GetValue<bool>($"{Constants.ConfigurationSection}:{nameof(ActiveMqBusOptions.Enabled)}");
		var connection = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(ActiveMqBusOptions.Connection)}");
		var name = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(ActiveMqBusOptions.Name)}") ?? Constants.DefaultTransportName;

		// Configures ActiveMQ message bus options from the application configuration.
		context.Services.Configure<ActiveMqBusOptions>(Configuration.GetSection(Constants.ConfigurationSection));

		if (enabled && !string.IsNullOrWhiteSpace(connection))
		{
			context.Services.AddActiveMqBus(name);
		}
	}
}