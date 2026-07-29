using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines configuration operations for the message bus, such as registering handlers,
/// configuring identity providers, conventions and transport strategies.
/// </summary>
public interface IBusConfigurator
{
	/// <summary>
	/// Registers message handlers found in the provided assemblies.
	/// </summary>
	/// <param name="assemblies">Assemblies to scan for handler types.</param>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator RegisterHandlers(params Assembly[] assemblies);

	/// <summary>
	/// Registers message handlers using a factory that returns handler types.
	/// The factory will be invoked to obtain the sequence of types to register.
	/// </summary>
	/// <param name="typesFactory">Factory function that returns handler types.</param>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator RegisterHandlers(Func<IEnumerable<Type>> typesFactory);

	/// <summary>
	/// Registers the specified handler types.
	/// </summary>
	/// <param name="types">Enumerable of handler types to register.</param>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator RegisterHandlers(IEnumerable<Type> types);

	/// <summary>
	/// Sets an identity accessor instance that will be used to obtain caller identity information.
	/// </summary>
	/// <param name="accessor">Identity accessor instance.</param>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator SetIdentityProvider(IdentityAccessor accessor);

	/// <summary>
	/// Configures the identity provider by specifying the provider type.
	/// The type must implement <see cref="IIdentityProvider"/> and will be resolved/instantiated
	/// by the underlying container or factory.
	/// </summary>
	/// <typeparam name="T">The identity provider implementation type.</typeparam>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator SetIdentityProvider<T>()
		where T : class, IIdentityProvider;

	/// <summary>
	/// Configures message conventions using the provided builder action.
	/// Conventions can influence how messages, handlers or topics are discovered and named.
	/// </summary>
	/// <param name="configure">Action that configures the <see cref="MessageConventionBuilder"/>.</param>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator SetConventions(Action<MessageConventionBuilder> configure);

	/// <summary>
	/// Configures a transport strategy for the specified transport name.
	/// </summary>
	/// <param name="transport">Transport name to configure (e.g. a transport implementation type or identifier).</param>
	/// <param name="configure">Action that configures the <see cref="TransportStrategyBuilder"/> for the transport.</param>
	/// <returns>The current <see cref="IBusConfigurator"/> for fluent configuration.</returns>
	IBusConfigurator SetStrategy(string transport, Action<TransportStrategyBuilder> configure);
}