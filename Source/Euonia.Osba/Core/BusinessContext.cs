using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Provides a contextual entry point for business logic execution.
/// Encapsulates access to the ambient <see cref="IServiceProvider"/>, the current user principal,
/// and helper methods to resolve services and create instances while propagating this
/// <see cref="BusinessContext"/> to created objects that implement <see cref="IUseBusinessContext"/>.
/// </summary>
public class BusinessContext
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BusinessContext"/> class.
	/// </summary>
	/// <param name="contextAccessor">Accessor that exposes the current <see cref="IServiceProvider"/>.</param>
	public BusinessContext(BusinessContextAccessor contextAccessor)
	{
		ContextAccessor = contextAccessor;
		User = contextAccessor.ServiceProvider.GetService<UserPrincipal>();
	}

	/// <summary>
	/// Gets the underlying <see cref="BusinessContextAccessor"/> used to obtain the current service provider.
	/// Internal to restrict access to the assembly.
	/// </summary>
	internal BusinessContextAccessor ContextAccessor { get; }

	/// <summary>
	/// Gets the current <see cref="ClaimsPrincipal"/> for the active user if available.
	/// Returns <c>null</c> when no user is set.
	/// </summary>
	public ClaimsPrincipal Principal => User?.Claims;

	/// <summary>
	/// Gets or sets the current <see cref="UserPrincipal"/> representing the application's user context.
	/// </summary>
	public UserPrincipal User { get; }

	/// <summary>
	/// Gets the current <see cref="IServiceProvider"/> from the <see cref="ContextAccessor"/>.
	/// May be <c>null</c> if no service provider is available.
	/// </summary>
	public IServiceProvider CurrentServiceProvider => ContextAccessor.ServiceProvider;

	/// <summary>
	/// Resolves a required service of type <typeparamref name="T"/> from the current service provider.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>The resolved service instance.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="CurrentServiceProvider"/> is <c>null</c>.</exception>
	public T GetRequiredService<T>()
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		var result = CurrentServiceProvider.GetRequiredService<T>();
		return result;
	}

	/// <summary>
	/// Resolves a required service of the specified <paramref name="serviceType"/> from the current service provider.
	/// </summary>
	/// <param name="serviceType">The service type to resolve.</param>
	/// <returns>The resolved service instance.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="CurrentServiceProvider"/> is <c>null</c>.</exception>
	public object GetRequiredService(Type serviceType)
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		return CurrentServiceProvider.GetRequiredService(serviceType);
	}

	/// <summary>
	/// Attempts to resolve a service of type <typeparamref name="T"/> from the current service provider.
	/// Returns <c>null</c> if the service is not registered.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>The resolved service instance or <c>null</c> if not found.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="CurrentServiceProvider"/> is <c>null</c>.</exception>
	public T GetService<T>()
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		var result = CurrentServiceProvider.GetService<T>();
		return result;
	}

	/// <summary>
	/// Attempts to resolve a service of the specified <paramref name="serviceType"/> from the current service provider.
	/// Returns <c>null</c> if the service is not registered.
	/// </summary>
	/// <param name="serviceType">The service type to resolve.</param>
	/// <returns>The resolved service instance or <c>null</c> if not found.</returns>
	/// <exception cref="NullReferenceException">Thrown when <see cref="CurrentServiceProvider"/> is <c>null</c>.</exception>
	public object GetService(Type serviceType)
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		{
		}

		return CurrentServiceProvider.GetService(serviceType);
	}

	/// <summary>
	/// Resolves a keyed service of type <typeparamref name="T"/> using the provided <paramref name="key"/>.
	/// Some DI containers allow registration of keyed services; this delegates to an extension helper.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="key">The key used to identify the specific registration. Must not be <c>null</c>.</param>
	/// <returns>The resolved keyed service instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c> (on .NET 5+).</exception>
	/// <exception cref="NullReferenceException">Thrown when <see cref="CurrentServiceProvider"/> is <c>null</c>.</exception>
	public T GetKeyedService<T>(object key)
	{
#if NET5_0_OR_GREATER
  ArgumentNullException.ThrowIfNull(key);
#else
		ArgumentAssert.ThrowIfNull(key, nameof(key));
#endif
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		{
		}

		return CurrentServiceProvider.GetKeyedService<T>(key);
	}

	/// <summary>
	/// Creates an instance of <typeparamref name="T"/> using the constructor that best matches the supplied <paramref name="parameters"/>.
	/// If a service provider is available, <see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/>
	/// is used so constructor injection can occur. If the created instance implements <see cref="IUseBusinessContext"/>,
	/// its <see cref="IUseBusinessContext.BusinessContext"/> is set to this context.
	/// </summary>
	/// <typeparam name="T">The concrete type to create.</typeparam>
	/// <param name="parameters">Constructor parameters to use for instantiation.</param>
	/// <returns>A new instance of <typeparamref name="T"/>.</returns>
	public T CreateInstance<T>(params object[] parameters)
	{
		return (T)CreateInstance(typeof(T), parameters);
	}

	/// <summary>
	/// Creates an instance of the specified <paramref name="objectType"/> using the constructor that best matches the supplied <paramref name="parameters"/>.
	/// If a service provider is available, <see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/>
	/// is used so constructor injection can occur. If the created instance implements <see cref="IUseBusinessContext"/>,
	/// its <see cref="IUseBusinessContext.BusinessContext"/> is set to this context.
	/// </summary>
	/// <param name="objectType">The concrete type to create.</param>
	/// <param name="parameters">Constructor parameters to use for instantiation.</param>
	/// <returns>A new instance of <paramref name="objectType"/>.</returns>
	public object CreateInstance(Type objectType, params object[] parameters)
	{
		object result;
		if (CurrentServiceProvider != null)
		{
			result = ActivatorUtilities.CreateInstance(CurrentServiceProvider, objectType, parameters);
		}
		else
		{
			result = Activator.CreateInstance(objectType, parameters);
		}

		if (result is IUseBusinessContext tmp)
		{
			tmp.BusinessContext = this;
		}

		return result;
	}

	/// <summary>
	/// Creates an instance of a generic type definition by supplying the generic type arguments.
	/// The <paramref name="type"/> parameter must be a generic type definition (e.g. typeof(Foo&lt;&gt;)).
	/// </summary>
	/// <param name="type">The generic type definition to instantiate.</param>
	/// <param name="paramTypes">The concrete type arguments to apply to the generic definition.</param>
	/// <returns>A new instance of the constructed generic type.</returns>
	public object CreateGenericInstance(Type type, params Type[] paramTypes)
	{
		var genericType = type.GetGenericTypeDefinition();
		var gt = genericType.MakeGenericType(paramTypes);
		return CreateInstance(gt);
	}
}