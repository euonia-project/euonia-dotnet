using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Module responsible for registering repository-related services into the dependency injection container.
/// </summary>
/// <remarks>
/// Implementations of <see cref="ModuleContextBase"/> can override <see cref="ConfigureServices"/>
/// to add services required by the repository functionality. This module registers a context provider
/// that repository components depend on.
/// </remarks>
public class RepositoryModule : ModuleContextBase
{
	/// <summary>
	/// Called during application/service configuration to register repository services.
	/// </summary>
	/// <param name="context">
	/// The <see cref="ServiceConfigurationContext"/> providing access to the current
	/// <see cref="IServiceCollection"/> used to register services.
	/// </param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		// Registers the repository context provider into the DI container.
		context.Services.AddContextProvider();
	}
}