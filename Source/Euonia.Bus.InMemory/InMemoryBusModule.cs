using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// Represents a module for configuring in-memory service bus components.
/// </summary>
public class InMemoryBusModule : ModuleContextBase
{
	/// <summary>
	/// Configures the in-memory bus options from the application configuration.
	/// </summary>
	/// <param name="context"></param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.Configure<InMemoryBusOptions>(Configuration.GetSection(Constants.ConfigurationSection));
	}

	/// <summary>
	/// Configures the services required for the in-memory service bus.
	/// </summary>
	/// <param name="context">
	/// The <see cref="ServiceConfigurationContext"/> that provides the service collection
	/// and other configuration details.
	/// </param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var name = Configuration.GetValue<string>($"{Constants.ConfigurationSection}:{nameof(InMemoryBusOptions.Name)}") ?? Constants.DefaultTransportName;
		context.Services.AddInMemoryBus(name, Configuration);
	}
}