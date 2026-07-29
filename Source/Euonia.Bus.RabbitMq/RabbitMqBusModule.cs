using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// Represents the RabbitMQ Bus Module, responsible for configuring services related to RabbitMQ messaging.
/// </summary>
public class RabbitMqBusModule : ModuleContextBase
{
	/// <summary>
	/// Configures the services required for RabbitMQ messaging.
	/// </summary>
	/// <param name="context">The service configuration context.</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var enabled = Configuration.GetValue<bool>($"{Constants.ConfigurationSection}:{nameof(RabbitMqBusOptions.Enabled)}");
		var connection = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(RabbitMqBusOptions.Connection)}");
		var name = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(RabbitMqBusOptions.Name)}") ?? Constants.DefaultTransportName;

		// Configures RabbitMQ message bus options from the application configuration.
		context.Services.Configure<RabbitMqBusOptions>(Configuration.GetSection(Constants.ConfigurationSection));

		if (enabled && !string.IsNullOrWhiteSpace(connection))
		{
			context.Services.AddRabbitMqBus(name);
		}
	}
}