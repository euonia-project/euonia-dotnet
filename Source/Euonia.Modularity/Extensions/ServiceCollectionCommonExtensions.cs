using Nerosoft.Euonia.Modularity;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionCommonExtensions
{
	/// <param name="services"></param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Determines whether the <see cref="IServiceCollection"/> contains a service of the specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool IsAdded<T>()
		{
			return services.IsAdded(typeof(T));
		}

		/// <summary>
		/// Determines whether the <see cref="IServiceCollection"/> contains a service of the specified type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool IsAdded(Type type)
		{
			return services.Any(d => d.ServiceType == type);
		}

		/// <summary>
		/// Determines whether the <see cref="IServiceCollection"/> contains a service of the specified service type and implementation type.
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <returns></returns>
		public bool IsAddedImplementation<TService, TImplementation>()
		{
			return services.IsAddedImplementation(typeof(TService), typeof(TImplementation));
		}

		/// <summary>
		/// Determines whether the <see cref="IServiceCollection"/> contains a service of the specified service type and implementation type.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="implementationType"></param>
		/// <returns></returns>
		public bool IsAddedImplementation(Type serviceType, Type implementationType)
		{
			return services.Any(d => d.ServiceType == serviceType && d.ImplementationType == implementationType);
		}

		/// <summary>
		/// Determines whether the <see cref="IServiceCollection"/> contains a service of the specified implementation type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool IsAddedImplementation<T>()
		{
			return services.IsAddedImplementation(typeof(T));
		}

		/// <summary>
		/// Determines whether the <see cref="IServiceCollection"/> contains a service of the specified implementation type.
		/// </summary>
		/// <param name="implementationType"></param>
		/// <returns></returns>
		public bool IsAddedImplementation(Type implementationType)
		{
			return services.Any(d => d.ImplementationType == implementationType);
		}

		/// <summary>
		/// Gets a singleton service object of the specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetSingletonInstanceOrNull<T>()
		{
			return (T)services
			          .FirstOrDefault(d => d.ServiceType == typeof(T))
			          ?.ImplementationInstance;
		}

		/// <summary>
		/// Gets a singleton service object of the specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public T GetSingletonInstance<T>()
		{
			var service = services.GetSingletonInstanceOrNull<T>();
			if (service == null)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_SINGLETON_SERVICE_NOT_FOUND, typeof(T).AssemblyQualifiedName));
			}

			return service;
		}

		/// <summary>
		/// Resolves a dependency using given <see cref="IServiceCollection"/>.
		/// This method should be used only after dependency injection registration phase completed.
		/// </summary>
		internal T GetService<T>()
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetService<T>();
		}

		/// <summary>
		/// Resolves a dependency using given <see cref="IServiceCollection"/>.
		/// This method should be used only after dependency injection registration phase completed.
		/// </summary>
		internal object GetService(Type type)
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetService(type);
		}

		/// <summary>
		/// Resolves a dependency using given <see cref="IServiceCollection"/>.
		/// Throws exception if service is not registered.
		/// This method should be used only after dependency injection registration phase completed.
		/// </summary>
		public T GetRequiredService<T>()
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetRequiredService<T>();
		}

		/// <summary>
		/// Resolves a dependency using given <see cref="IServiceCollection"/>.
		/// Throws exception if service is not registered.
		/// This method should be used only after dependency injection registration phase completed.
		/// </summary>
		public object GetRequiredService(Type type)
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetRequiredService(type);
		}

		/// <summary>
		/// Returns a <see cref="Lazy{T}"/> to resolve a service from given <see cref="IServiceCollection"/>
		/// once dependency injection registration phase completed.
		/// </summary>
		public Lazy<T> GetServiceLazy<T>()
		{
			return new Lazy<T>(services.GetService<T>, true);
		}

		/// <summary>
		/// Returns a <see cref="Lazy{T}"/> to resolve a service from given <see cref="IServiceCollection"/>
		/// once dependency injection registration phase completed.
		/// </summary>
		public Lazy<object> GetServiceLazy(Type type)
		{
			return new Lazy<object>(() => services.GetService(type), true);
		}

		/// <summary>
		/// Returns a <see cref="Lazy{T}"/> to resolve a service from given <see cref="IServiceCollection"/>
		/// once dependency injection registration phase completed.
		/// </summary>
		public Lazy<T> GetRequiredServiceLazy<T>()
		{
			return new Lazy<T>(services.GetRequiredService<T>, true);
		}

		/// <summary>
		/// Returns a <see cref="Lazy{T}"/> to resolve a service from given <see cref="IServiceCollection"/>
		/// once dependency injection registration phase completed.
		/// </summary>
		public Lazy<object> GetRequiredServiceLazy(Type type)
		{
			return new Lazy<object>(() => services.GetRequiredService(type), true);
		}

		/// <summary>
		/// Gets the <see cref="IServiceProvider"/> from the <see cref="IServiceCollection"/>, or null if not registered.
		/// </summary>
		/// <returns></returns>
		public IServiceProvider GetServiceProviderOrNull()
		{
			return services.GetObjectOrNull<IServiceProvider>();
		}

		/// <summary>
		/// Gets the <see cref="IServiceProvider"/> from the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ObjectAccessor<T> TryAddObjectAccessor<T>()
		{
			if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
			{
				return services.GetSingletonInstance<ObjectAccessor<T>>();
			}

			return services.AddObjectAccessor<T>();
		}

		/// <summary>
		/// Add an empty <see cref="ObjectAccessor{T}"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ObjectAccessor<T> AddObjectAccessor<T>()
		{
			return services.AddObjectAccessor(new ObjectAccessor<T>());
		}

		/// <summary>
		/// Add an object as <see cref="ObjectAccessor{T}"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="obj"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ObjectAccessor<T> AddObjectAccessor<T>(T obj)
		{
			return services.AddObjectAccessor(new ObjectAccessor<T>(obj));
		}

		/// <summary>
		/// Add an <see cref="ObjectAccessor{T}"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="accessor"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public ObjectAccessor<T> AddObjectAccessor<T>(ObjectAccessor<T> accessor)
		{
			if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
			{
				throw new Exception(string.Format(Resources.IDS_OBJECT_ACCESSOR_ALREADY_REGISTERED, typeof(T).AssemblyQualifiedName));
			}

			//Add to the beginning for fast retrieve
			services.Insert(0, ServiceDescriptor.Singleton(typeof(ObjectAccessor<T>), accessor));
			services.Insert(0, ServiceDescriptor.Singleton(typeof(IObjectAccessor<T>), accessor));

			return accessor;
		}

		/// <summary>
		/// Gets the object from the <see cref="IServiceCollection"/>, or null if not registered.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetObjectOrNull<T>()
			where T : class
		{
			return services.GetSingletonInstanceOrNull<IObjectAccessor<T>>()?.Value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public T GetObject<T>()
			where T : class
		{
			return services.GetObjectOrNull<T>() ?? throw new Exception(string.Format(Resources.IDS_OBJECT_NOT_FOUND_IN_SERVICES, typeof(T).AssemblyQualifiedName));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="registrar"></param>
		/// <returns></returns>
		public IServiceCollection AddAutomaticRegistrar(IAutomaticRegistration registrar)
		{
			GetOrCreateRegistrars(services).Add(registrar);
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddAssemblyOf<T>()
		{
			return services.AddAssembly(typeof(T).GetTypeInfo().Assembly);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public IServiceCollection AddAssembly(Assembly assembly)
		{
			foreach (var registrar in GetOrCreateRegistrars(services))
			{
				registrar.AddAssembly(services, assembly);
			}

			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="registrationAction"></param>
		public void Registry(Action<IServiceRegistrationContext> registrationAction)
		{
			GetOrCreateRegistrationActions(services).Add(registrationAction);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="exposeAction"></param>
		public void OnExposing(Action<IServiceExposingContext> exposeAction)
		{
			GetOrCreateExposing(services).Add(exposeAction);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ServiceExposingAction GetExposingActions()
		{
			return GetOrCreateExposing(services);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		public IServiceCollection AddProxiedScoped<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			services.AddScoped<TImplementation>();
			services.AddScoped(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="implementationType"></param>
		/// <returns></returns>
		public IServiceCollection AddProxiedScoped(Type serviceType, Type implementationType)
		{
			services.AddScoped(implementationType);
			services.AddScoped(serviceType, provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService(implementationType);
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(serviceType, actual, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="interceptors"></param>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedScoped<TService, TImplementation>(IEnumerable<IInterceptor> interceptors)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddScoped<TImplementation>();
			services.AddScoped(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors?.ToArray() ?? []);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="interceptorTypes"></param>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedScoped<TService, TImplementation>(IEnumerable<Type> interceptorTypes)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddScoped<TImplementation>();
			services.AddScoped(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				var interceptors = interceptorTypes?.Select(type => (IInterceptor)ActivatorUtilities.GetServiceOrCreateInstance(provider, type)).ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors ?? []);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		public IServiceCollection AddProxiedSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			services.AddSingleton<TImplementation>();
			services.AddSingleton(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="implementationType"></param>
		/// <returns></returns>
		public IServiceCollection AddProxiedSingleton(Type serviceType, Type implementationType)
		{
			services.AddSingleton(implementationType);
			services.AddSingleton(serviceType, provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService(implementationType);
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(serviceType, actual, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="implementation"></param>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedSingleton<TService>(TService implementation)
		{
			services.AddSingleton(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), implementation, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="interceptors"></param>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedSingleton<TService, TImplementation>(IEnumerable<IInterceptor> interceptors)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddSingleton<TImplementation>();
			services.AddSingleton(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors?.ToArray() ?? []);
			});
			return services;
		}

		/// <summary>
		/// Adds a proxied service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> using singleton lifetime.
		/// </summary>
		/// <param name="interceptorTypes"></param>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedSingleton<TService, TImplementation>(IEnumerable<Type> interceptorTypes)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddSingleton<TImplementation>();
			services.AddSingleton(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				var interceptors = interceptorTypes?.Select(type => (IInterceptor)ActivatorUtilities.GetServiceOrCreateInstance(provider, type)).ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors ?? []);
			});
			return services;
		}

		/// <summary>
		/// Adds a proxied service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> using transient lifetime.
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		public IServiceCollection AddProxiedTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			services.AddTransient<TImplementation>();
			services.AddTransient(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors);
			});
			return services;
		}

		/// <summary>
		/// Adds a proxied service of the type specified in <paramref name="serviceType" /> to the specified <see cref="IServiceCollection" /> using transient lifetime.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="implementationType"></param>
		/// <returns></returns>
		public IServiceCollection AddProxiedTransient(Type serviceType, Type implementationType)
		{
			services.AddTransient(implementationType);
			services.AddTransient(serviceType, provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService(implementationType);
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(serviceType, actual, interceptors);
			});
			return services;
		}

		/// <summary>
		/// Adds a proxied service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> using transient lifetime.
		/// </summary>
		/// <param name="implementation"></param>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedTransient<TService>(TService implementation)
		{
			services.AddTransient(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), implementation, interceptors);
			});
			return services;
		}

		/// <summary>
		/// Adds a proxied service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> using transient lifetime.
		/// </summary>
		/// <param name="interceptors"></param>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		public IServiceCollection AddProxiedTransient<TService, TImplementation>(IEnumerable<IInterceptor> interceptors)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddTransient<TImplementation>();
			services.AddTransient(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors?.ToArray() ?? []);
			});
			return services;
		}

		/// <summary>
		/// Adds a proxied service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> using transient lifetime.
		/// </summary>
		/// <param name="interceptorTypes"></param>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddProxiedTransient<TService, TImplementation>(IEnumerable<Type> interceptorTypes)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddTransient<TImplementation>();
			services.AddTransient(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var actual = provider.GetRequiredService<TImplementation>();
				var interceptors = interceptorTypes?.Select(type => (IInterceptor)ActivatorUtilities.GetServiceOrCreateInstance(provider, type)).ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), actual, interceptors ?? []);
			});
			return services;
		}

		/// <summary>
		/// Adds a transient service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> with a given name.
		/// </summary>
		/// <param name="name">The name of service implementation.</param>
		/// <typeparam name="TService">The service type.</typeparam>
		/// <typeparam name="TImplementation">The implementation type.</typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Throws if the <typeparamref name="TService"/> with given name already registered.</exception>
		public IServiceCollection AddTransient<TService, TImplementation>(string name)
			where TImplementation : class, TService
		{
			if (!Singleton<NamedServiceContainer<TService>>.Instance.TryAdd(name, typeof(TImplementation)))
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_NAMED_SERVICE_ALREADY_REGISTERED, nameof(TService), name));
			}

			services.AddNamedService((key, provider) =>
			{
				if (!Singleton<NamedServiceContainer<TService>>.Instance.TryGetValue(key, out var type))
				{
					return default;
				}

				return (TService)ActivatorUtilities.GetServiceOrCreateInstance(provider, type);
			});
			services.AddTransient<TImplementation>();

			return services;
		}

		/// <summary>
		/// Adds a scoped service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> with a given name.
		/// </summary>
		/// <param name="name">The name of service implementation.</param>
		/// <typeparam name="TService">The service type.</typeparam>
		/// <typeparam name="TImplementation">The implementation type.</typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Throws if the <typeparamref name="TService"/> with given name already registered.</exception>
		public IServiceCollection AddScoped<TService, TImplementation>(string name)
			where TImplementation : class, TService
		{
			if (!Singleton<NamedServiceContainer<TService>>.Instance.TryAdd(name, typeof(TImplementation)))
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_NAMED_SERVICE_ALREADY_REGISTERED, nameof(TService), name));
			}

			services.AddNamedService((key, provider) =>
			{
				if (!Singleton<NamedServiceContainer<TService>>.Instance.TryGetValue(key, out var type))
				{
					return default;
				}

				return (TService)ActivatorUtilities.GetServiceOrCreateInstance(provider, type);
			});
			services.AddScoped<TImplementation>();

			return services;
		}

		/// <summary>
		/// Adds a singleton service of the type specified in <typeparamref name="TService" /> to the specified <see cref="IServiceCollection" /> with a given name.
		/// </summary>
		/// <param name="name">The name of service implementation.</param>
		/// <typeparam name="TService">The service type.</typeparam>
		/// <typeparam name="TImplementation">The implementation type.</typeparam>
		/// <returns>A reference to this instance after the operation has completed.</returns>
		/// <exception cref="InvalidOperationException">Throws if the <typeparamref name="TService"/> with given name already registered.</exception>
		public IServiceCollection AddSingleton<TService, TImplementation>(string name)
			where TImplementation : class, TService
		{
			if (!Singleton<NamedServiceContainer<TService>>.Instance.TryAdd(name, typeof(TImplementation)))
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_NAMED_SERVICE_ALREADY_REGISTERED, nameof(TService), name));
			}

			services.AddNamedService((key, provider) =>
			{
				if (!Singleton<NamedServiceContainer<TService>>.Instance.TryGetValue(key, out var type))
				{
					return default;
				}

				return (TService)ActivatorUtilities.GetServiceOrCreateInstance(provider, type);
			});
			services.AddSingleton<TImplementation>();

			return services;
		}

		/// <summary>
		/// Adds service of the type <typeparamref name="TService" /> using the factory to the specified <see cref="IServiceCollection" />.
		/// </summary>
		/// <param name="factory">The factory to create a implementation instance of <typeparamref name="TService"/>.</param>
		/// <typeparam name="TService">The service type.</typeparam>
		/// <returns>A reference to this instance after the operation has completed.</returns>
		public IServiceCollection AddNamedService<TService>(Func<string, IServiceProvider, TService> factory)
		{
			services.TryAddTransient<NamedService<TService>>(provider =>
			{
				return name => factory(name, provider);
			});
			return services;
		}
	}

	private static ConventionalRegistrarList GetOrCreateRegistrars(IServiceCollection services)
	{
		var conventionalRegistrars = services.GetSingletonInstanceOrNull<IObjectAccessor<ConventionalRegistrarList>>()?.Value;
		if (conventionalRegistrars == null)
		{
			conventionalRegistrars = new ConventionalRegistrarList { new DefaultAutomaticRegistration() };
			services.AddObjectAccessor(conventionalRegistrars);
		}

		{
		}
		return conventionalRegistrars;
	}

	private static ServiceExposingAction GetOrCreateExposing(IServiceCollection services)
	{
		var actionList = services.GetSingletonInstanceOrNull<IObjectAccessor<ServiceExposingAction>>()?.Value;
		if (actionList != null)
		{
			return actionList;
		}

		actionList = new ServiceExposingAction();
		services.AddObjectAccessor(actionList);

		return actionList;
	}

	private static ServiceRegistrationAction GetOrCreateRegistrationActions(IServiceCollection services)
	{
		var actionList = services.GetSingletonInstanceOrNull<IObjectAccessor<ServiceRegistrationAction>>()?.Value;
		if (actionList == null)
		{
			actionList = new ServiceRegistrationAction();
			services.AddObjectAccessor(actionList);
		}

		return actionList;
	}
}