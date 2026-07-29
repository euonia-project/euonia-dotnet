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
			services.TryAddTransient<InMemoryQueueConsumer>();

			// Registers the in-memory topic subscriber as a transient service.
			services.TryAddTransient<InMemoryTopicSubscriber>();

			// Registers the in-memory transport as a singleton service.
			services.TryAddSingleton<InMemoryTransport>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransport) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransport>(name, (provider, _) => provider.GetRequiredService<InMemoryTransport>());
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