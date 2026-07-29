using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Repository;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding repository services to the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds default context provider.
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddContextProvider(this IServiceCollection services)
	{
		services.TryAddScoped<IContextProvider, DefaultContextProvider>();
		services.AddTransient<IContextFactory, DefaultContextFactory>();
		return services;
	}
}