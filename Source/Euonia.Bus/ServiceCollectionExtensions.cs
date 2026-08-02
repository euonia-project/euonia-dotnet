using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
		public IServiceCollection AddEuoniaBus(Action<DefaultConfigurator> config = null)
		{
			services.AddSingleton<IConfigurator>(provider =>
			{
				var configurator = ActivatorUtilities.GetServiceOrCreateInstance<DefaultConfigurator>(provider);
				config?.Invoke(configurator);
				return configurator;
			});

			var handlerTypes = ChannelRegistrar.Registrations
			                                   .SelectMany(t => t.Value.Handlers)
			                                   .Select(t => t.HandlerType)
			                                   .Distinct()
			                                   .ToList();

			foreach (var handlerType in handlerTypes)
			{
				services.TryAddTransient(handlerType);
			}

			services.AddPipeline();

			services.TryAddSingleton<IHandlerContext>(provider =>
			{
				var configurator = provider.GetRequiredService<IConfigurator>();
				var context = new DefaultHandlerContext(provider);

				var registerMethod = typeof(DefaultHandlerContext).GetMethod(nameof(DefaultHandlerContext.Register), 3, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, [typeof(string)]);

				foreach (var (channel, registration) in configurator.Registrations)
				{
					foreach (var handler in registration.Handlers)
					{
						Type responseType = null;
						if (handler.Method.ReturnType.IsGenericType && handler.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
						{
							responseType = handler.Method.ReturnType.GenericTypeArguments[0];
						}

						if (responseType != null && handler.HandlerType.IsAssignableTo(typeof(IHandler<,>).MakeGenericType(registration.MessageType, responseType)))
						{
							registerMethod?.MakeGenericMethod(registration.MessageType, responseType, handler.HandlerType).Invoke(context, [channel]);
						}
						else
						{
							context.Register(channel, handler);
						}
					}
				}

				return context;
			});

			// services.TryAddTransient<IMessageBusOptions>(provider =>
			// {
			// 	var options = provider.CreateScope().ServiceProvider.GetService<IOptionsSnapshot<MessageBusOptions>>();
			// 	return options?.Value ?? new MessageBusOptions();
			// });
			services.TryAddSingleton<IBus, MessageBus>();
			services.TryAddSingleton<IDispatcher, StrategicDispatcher>();
			services.AddHostedService<RecipientActivator>();

			return services;
		}
	}
}