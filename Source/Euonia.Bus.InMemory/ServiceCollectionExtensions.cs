using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for IServiceCollection to add In-Memory Bus services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services"></param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds the in-memory bus services to the service collection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="configuration"></param>
		/// <param name="configureOptions"></param>
		/// <returns></returns>
		public IServiceCollection AddInMemoryBus(string name, IConfiguration configuration, Action<InMemoryBusOptions> configureOptions = null)
		{
			if (configureOptions != null)
			{
				services.Configure(configureOptions);
			}

			// Registers the in-memory queue consumer as a transient service.
			services.TryAddTransient<InMemoryConsumer>();

			// Registers the in-memory topic subscriber as a transient service.
			services.TryAddTransient<InMemorySubscriber>();

			// Registers the in-memory transport as a singleton service.
			services.TryAddSingleton<InMemoryTransporter>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransporter>(name, (provider, _) => provider.GetRequiredService<InMemoryTransporter>());
			}

			// Registers the in-memory recipient registrar as a transient service
			// implementing the IRecipientRegistrar interface.
			if (!services.IsAddedImplementation<IRecipientRegistrar, InMemoryRecipientRegistrar>())
			{
				services.AddTransient<IRecipientRegistrar, InMemoryRecipientRegistrar>();
			}
			return services;
		}
	}
}