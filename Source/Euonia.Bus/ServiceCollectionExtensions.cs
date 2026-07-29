using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for service collection to add message bus.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">The <see cref="IServiceCollection"/> inatance.</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds the message bus to the service collection.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IServiceCollection AddEuoniaBus(Action<IBusConfigurator> config = null)
		{
			var configurator = Singleton<BusConfigurator>.Get(() => new BusConfigurator(services));

			config?.Invoke(configurator);

			var handlerTypes = HandlerRegistrar.Registrations
			                                   .Select(t => t.HandlerType)
			                                   .Distinct()
			                                   .ToList();

			foreach (var handlerType in handlerTypes)
			{
				services.TryAddTransient(handlerType);
			}

			services.AddPipeline();

			services.AddSingleton<IBusConfigurator>(_ => configurator);

			services.TryAddSingleton<IHandlerContext>(provider =>
			{
				var context = new HandlerContext(provider);

				var registerMethod = typeof(HandlerContext).GetMethod(nameof(HandlerContext.Register), 3, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, []);

				foreach (var registration in HandlerRegistrar.Registrations)
				{
					Type responseType = null;
					if (registration.Method.ReturnType.IsGenericType && registration.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
					{
						responseType = registration.Method.ReturnType.GenericTypeArguments[0];
					}

					if (responseType != null && registration.HandlerType.IsAssignableTo(typeof(IHandler<,>).MakeGenericType(registration.MessageType, responseType)))
					{
						registerMethod?.MakeGenericMethod(registration.MessageType, responseType, registration.HandlerType).Invoke(context, null);
					}
					else
					{
						context.Register(registration);
					}
				}

				return context;
			});

			services.TryAddTransient<IMessageBusOptions>(provider =>
			{
				var options = provider.CreateScope().ServiceProvider.GetService<IOptionsSnapshot<MessageBusOptions>>();
				return options?.Value ?? new MessageBusOptions();
			});
			services.TryAddTransient<IBus, MessageBus>();
			services.TryAddSingleton<IDispatcher, StrategicDispatcher>();
			services.AddHostedService<RecipientActivator>();

			return services;
		}
	}
}