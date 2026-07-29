using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The service bus module.
/// </summary>
public class MessageBusModule : ModuleContextBase
{
	/// <inheritdoc />
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddOptions<MessageBusOptions>()
						.BindConfiguration(Constants.ConfigurationSection)
						.Validate(_ => true);
	}

	/// <inheritdoc />
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddEuoniaBus();
	}

	/// <inheritdoc />
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
	}
}