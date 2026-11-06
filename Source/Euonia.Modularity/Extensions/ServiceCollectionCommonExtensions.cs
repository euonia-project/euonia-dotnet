using Nerosoft.Euonia.Modularity;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> 的扩展方法。
/// </summary>
public static class ServiceCollectionCommonExtensions
{
	/// <summary>
	/// 定义 <see cref="IServiceCollection"/> 的扩展成员。
	/// </summary>
	/// <param name="services">要扩展的服务集合实例。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 确定 <see cref="IServiceCollection"/> 是否包含指定类型的服务。
		/// </summary>
		/// <typeparam name="T">要检查的服务类型。</typeparam>
		/// <returns>如果存在服务类型为 <typeparamref name="T"/> 的注册，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
		/// <remarks>此方法只比较服务类型，不校验实现类型或生命周期。</remarks>
		public bool IsAdded<T>()
		{
			return services.IsAdded(typeof(T));
		}

		/// <summary>
		/// 确定 <see cref="IServiceCollection"/> 是否包含指定类型的服务。
		/// </summary>
		/// <param name="type">要检查的服务类型。</param>
		/// <returns>如果存在服务类型等于 <paramref name="type"/> 的注册，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
		public bool IsAdded(Type type)
		{
			return services.Any(d => d.ServiceType == type);
		}

		/// <summary>
		/// 确定 <see cref="IServiceCollection"/> 是否包含指定服务类型和实现类型的服务。
		/// </summary>
		/// <typeparam name="TService">要检查的服务类型。</typeparam>
		/// <typeparam name="TImplementation">要检查的实现类型。</typeparam>
		/// <returns>如果同时存在服务类型为 <typeparamref name="TService"/> 且实现类型为 <typeparamref name="TImplementation"/> 的注册，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
		public bool IsAddedImplementation<TService, TImplementation>()
		{
			return services.IsAddedImplementation(typeof(TService), typeof(TImplementation));
		}

		/// <summary>
		/// 确定 <see cref="IServiceCollection"/> 是否包含指定服务类型和实现类型的服务。
		/// </summary>
		/// <param name="serviceType">要检查的服务类型。</param>
		/// <param name="implementationType">要检查的实现类型。</param>
		/// <returns>如果同时存在服务类型为 <paramref name="serviceType"/> 且实现类型为 <paramref name="implementationType"/> 的注册，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
		public bool IsAddedImplementation(Type serviceType, Type implementationType)
		{
			return services.Any(d => d.ServiceType == serviceType && d.ImplementationType == implementationType);
		}

		/// <summary>
		/// 确定 <see cref="IServiceCollection"/> 是否包含指定实现类型的服务。
		/// </summary>
		/// <typeparam name="T">要检查的实现类型。</typeparam>
		/// <returns>如果存在实现类型为 <typeparamref name="T"/> 的注册，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
		public bool IsAddedImplementation<T>()
		{
			return services.IsAddedImplementation(typeof(T));
		}

		/// <summary>
		/// 确定 <see cref="IServiceCollection"/> 是否包含指定实现类型的服务。
		/// </summary>
		/// <param name="implementationType">要检查的实现类型。</param>
		/// <returns>如果存在实现类型为 <paramref name="implementationType"/> 的注册，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
		public bool IsAddedImplementation(Type implementationType)
		{
			return services.Any(d => d.ImplementationType == implementationType);
		}

		/// <summary>
		/// 获取指定类型的单例服务对象。
		/// </summary>
		/// <typeparam name="T">要获取的服务类型。</typeparam>
		/// <returns>第一个服务类型为 <typeparamref name="T"/> 的注册所持有的实例；若未找到该类型，或该注册使用工厂创建实例，则返回 <see langword="null"/>。</returns>
		/// <remarks>此方法只读取 <see cref="ServiceDescriptor.ImplementationInstance"/>，不会通过容器解析服务。</remarks>
		public T? GetSingletonInstanceOrNull<T>()
		{
			return (T?)services
			          .FirstOrDefault(d => d.ServiceType == typeof(T))
			          ?.ImplementationInstance;
		}

		/// <summary>
		/// 获取指定类型的单例服务对象。
		/// </summary>
		/// <typeparam name="T">要获取的服务类型。</typeparam>
		/// <returns>服务集合中已注册的单例实例。</returns>
		/// <exception cref="InvalidOperationException">当集合中不存在服务类型为 <typeparamref name="T"/> 的单例实例时抛出。</exception>
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
		/// 使用给定的 <see cref="IServiceCollection"/> 解析依赖。
		/// 此方法只应在依赖注入注册阶段完成后使用。
		/// </summary>
		internal T? GetService<T>()
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetService<T>();
		}

		/// <summary>
		/// 使用给定的 <see cref="IServiceCollection"/> 解析依赖。
		/// 此方法只应在依赖注入注册阶段完成后使用。
		/// </summary>
		internal object? GetService(Type type)
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetService(type);
		}

		/// <summary>
		/// 使用给定的 <see cref="IServiceCollection"/> 解析依赖。
		/// 如果服务未注册则抛出异常。
		/// 此方法只应在依赖注入注册阶段完成后使用。
		/// </summary>
		public T GetRequiredService<T>()
		{
			return (T)services
			          .GetSingletonInstance<IModularityApplication>()
			          .ServiceProvider
			          .GetRequiredService(typeof(T));
		}

		/// <summary>
		/// 使用给定的 <see cref="IServiceCollection"/> 解析依赖。
		/// 如果服务未注册则抛出异常。
		/// 此方法只应在依赖注入注册阶段完成后使用。
		/// </summary>
		public object GetRequiredService(Type type)
		{
			return services
			       .GetSingletonInstance<IModularityApplication>()
			       .ServiceProvider
			       .GetRequiredService(type);
		}

		/// <summary>
		/// 返回 <see cref="Lazy{T}"/>，以便在依赖注入注册阶段完成后
		/// 从给定的 <see cref="IServiceCollection"/> 解析服务。
		/// </summary>
		public Lazy<T?> GetServiceLazy<T>()
		{
			return new Lazy<T?>(services.GetService<T>, true);
		}

		/// <summary>
		/// 返回 <see cref="Lazy{T}"/>，以便在依赖注入注册阶段完成后
		/// 从给定的 <see cref="IServiceCollection"/> 解析服务。
		/// </summary>
		public Lazy<object?> GetServiceLazy(Type type)
		{
			return new Lazy<object?>(() => services.GetService(type), true);
		}

		/// <summary>
		/// 返回 <see cref="Lazy{T}"/>，以便在依赖注入注册阶段完成后
		/// 从给定的 <see cref="IServiceCollection"/> 解析服务。
		/// </summary>
		public Lazy<T> GetRequiredServiceLazy<T>()
		{
			return new Lazy<T>(services.GetRequiredService<T>, true);
		}

		/// <summary>
		/// 返回 <see cref="Lazy{T}"/>，以便在依赖注入注册阶段完成后
		/// 从给定的 <see cref="IServiceCollection"/> 解析服务。
		/// </summary>
		public Lazy<object> GetRequiredServiceLazy(Type type)
		{
			return new Lazy<object>(() => services.GetRequiredService(type), true);
		}

		/// <summary>
		/// 从 <see cref="IServiceCollection"/> 获取 <see cref="IServiceProvider"/>，若未注册则返回 null。
		/// </summary>
		/// <returns>已注册的 <see cref="IServiceProvider"/>；若尚未注册则返回 <see langword="null"/>。</returns>
		public IServiceProvider? GetServiceProviderOrNull()
		{
			return services.GetObjectOrNull<IServiceProvider>();
		}

		/// <summary>
		/// 确保 <see cref="IServiceCollection"/> 中存在指定对象类型的 <see cref="ObjectAccessor{T}"/>。
		/// </summary>
		/// <typeparam name="T">访问器承载的对象类型。</typeparam>
		/// <returns>已存在的或新添加的 <see cref="ObjectAccessor{T}"/> 实例。</returns>
		public ObjectAccessor<T> TryAddObjectAccessor<T>()
		{
			if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
			{
				return services.GetSingletonInstance<ObjectAccessor<T>>();
			}

			return services.AddObjectAccessor<T>();
		}

		/// <summary>
		/// 向 <see cref="IServiceCollection"/> 添加一个空的 <see cref="ObjectAccessor{T}"/>。
		/// </summary>
		/// <typeparam name="T">访问器承载的对象类型。</typeparam>
		/// <returns>新添加的、值保持默认的访问器实例。</returns>
		public ObjectAccessor<T> AddObjectAccessor<T>()
		{
			return services.AddObjectAccessor(new ObjectAccessor<T>());
		}

		/// <summary>
		/// 将一个对象作为 <see cref="ObjectAccessor{T}"/> 添加到 <see cref="IServiceCollection"/>。
		/// </summary>
		/// <param name="obj">要作为访问器初始值的对象。</param>
		/// <typeparam name="T">访问器承载的对象类型。</typeparam>
		/// <returns>包装了 <paramref name="obj"/> 的新访问器实例。</returns>
		public ObjectAccessor<T> AddObjectAccessor<T>(T obj)
		{
			return services.AddObjectAccessor(new ObjectAccessor<T>(obj));
		}

		/// <summary>
		/// 向 <see cref="IServiceCollection"/> 添加 <see cref="ObjectAccessor{T}"/>。
		/// </summary>
		/// <param name="accessor">要注册的访问器实例。</param>
		/// <typeparam name="T">访问器承载的对象类型。</typeparam>
		/// <returns>传入的 <paramref name="accessor"/> 实例。</returns>
		/// <exception cref="Exception">当 <see cref="ObjectAccessor{T}"/> 已注册到服务集合中时抛出。</exception>
		/// <remarks>访问器会同时以 <see cref="ObjectAccessor{T}"/> 和 <see cref="IObjectAccessor{T}"/> 注册为单例，并插入到集合开头以便快速检索。</remarks>
		public ObjectAccessor<T> AddObjectAccessor<T>(ObjectAccessor<T> accessor)
		{
			if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
			{
				throw new Exception(string.Format(Resources.IDS_OBJECT_ACCESSOR_ALREADY_REGISTERED, typeof(T).AssemblyQualifiedName));
			}

			//添加到开头以便快速检索
			services.Insert(0, ServiceDescriptor.Singleton(typeof(ObjectAccessor<T>), accessor));
			services.Insert(0, ServiceDescriptor.Singleton(typeof(IObjectAccessor<T>), accessor));

			return accessor;
		}

		/// <summary>
		/// 从 <see cref="IServiceCollection"/> 获取对象，若未注册则返回 null。
		/// </summary>
		/// <typeparam name="T">要获取的对象类型。</typeparam>
		/// <returns>已注册的对象；若不存在对应的 <see cref="IObjectAccessor{T}"/>，或访问器的值为 <see langword="null"/>，则返回 <see langword="null"/>。</returns>
		public T? GetObjectOrNull<T>()
			where T : class
		{
			return services.GetSingletonInstanceOrNull<IObjectAccessor<T>>()?.Value;
		}

		/// <summary>
		/// 从 <see cref="IServiceCollection"/> 获取对象，若未注册则抛出异常。
		/// </summary>
		/// <typeparam name="T">要获取的对象类型。</typeparam>
		/// <returns>已注册的对象。</returns>
		/// <exception cref="Exception">当集合中不存在 <typeparamref name="T"/> 类型的对象访问器或对象时抛出。</exception>
		public T GetObject<T>()
			where T : class
		{
			return services.GetObjectOrNull<T>() ?? throw new Exception(string.Format(Resources.IDS_OBJECT_NOT_FOUND_IN_SERVICES, typeof(T).AssemblyQualifiedName));
		}

		/// <summary>
		/// 向服务集合注册一个自动注册器，用于后续按程序集或类型批量注册服务。
		/// </summary>
		/// <param name="registrar">要添加的自动注册实现。</param>
		/// <returns>操作完成后对此实例的引用。</returns>
		public IServiceCollection AddAutomaticRegistrar(IAutomaticRegistration registrar)
		{
			GetOrCreateRegistrars(services).Add(registrar);
			return services;
		}

		/// <summary>
		/// 将 <typeparamref name="T"/> 所在程序集中的类型添加到服务集合。
		/// </summary>
		/// <typeparam name="T">用于定位目标程序集的类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		public IServiceCollection AddAssemblyOf<T>()
		{
			return services.AddAssembly(typeof(T).GetTypeInfo().Assembly);
		}

		/// <summary>
		/// 遍历已注册的自动注册器，将指定程序集中的类型添加到服务集合。
		/// </summary>
		/// <param name="assembly">要扫描类型并注册的程序集。</param>
		/// <returns>操作完成后对此实例的引用。</returns>
		public IServiceCollection AddAssembly(Assembly assembly)
		{
			foreach (var registrar in GetOrCreateRegistrars(services))
			{
				registrar.AddAssembly(services, assembly);
			}

			return services;
		}

		/// <summary>
		/// 注册一个服务注册回调，回调保存在服务集合的注册操作列表中。
		/// </summary>
		/// <param name="registrationAction">接收 <see cref="IServiceRegistrationContext"/> 的服务注册回调。</param>
		public void Registry(Action<IServiceRegistrationContext> registrationAction)
		{
			GetOrCreateRegistrationActions(services).Add(registrationAction);
		}

		/// <summary>
		/// 注册一个服务公开回调，用于在自动注册时调整实现类型所公开的服务类型。
		/// </summary>
		/// <param name="exposeAction">接收 <see cref="IServiceExposingContext"/> 的服务公开回调。</param>
		public void OnExposing(Action<IServiceExposingContext> exposeAction)
		{
			GetOrCreateExposing(services).Add(exposeAction);
		}

		/// <summary>
		/// 获取服务集合中的服务公开操作列表。
		/// </summary>
		/// <returns>已注册的 <see cref="ServiceExposingAction"/>；若不存在则创建空列表、注册后返回。</returns>
		public ServiceExposingAction GetExposingActions()
		{
			return GetOrCreateExposing(services);
		}

		/// <summary>
		/// 使用作用域生命周期注册 <typeparamref name="TService"/> 的接口代理，代理目标为 <typeparamref name="TImplementation"/>。
		/// </summary>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为作用域服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>代理在首次解析时创建，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
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
		/// 使用作用域生命周期注册 <paramref name="serviceType"/> 的接口代理，代理目标为 <paramref name="implementationType"/>。
		/// </summary>
		/// <param name="serviceType">要注册的服务接口类型。</param>
		/// <param name="implementationType">实现类型，同时以自身类型注册为作用域服务。</param>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>代理在首次解析时创建，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
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
		/// 使用作用域生命周期注册 <typeparamref name="TService"/> 的接口代理，并应用指定的拦截器。
		/// </summary>
		/// <param name="interceptors">应用于代理的拦截器集合；为 <see langword="null"/> 时按空集合处理，不再从容器解析 <see cref="IInterceptor"/>。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为作用域服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks><paramref name="interceptors"/> 在代理创建时才枚举。</remarks>
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
		/// 使用作用域生命周期注册 <typeparamref name="TService"/> 的接口代理，并按类型解析拦截器。
		/// </summary>
		/// <param name="interceptorTypes">拦截器类型集合，创建代理时通过 <see cref="ActivatorUtilities.GetServiceOrCreateInstance(IServiceProvider, Type)"/> 获取或创建实例；为 <see langword="null"/> 时按空集合处理。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为作用域服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
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
		/// 使用单例生命周期注册 <typeparamref name="TService"/> 的接口代理，代理目标为 <typeparamref name="TImplementation"/>。
		/// </summary>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为单例服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>代理在首次解析时创建，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
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
		/// 使用单例生命周期注册 <paramref name="serviceType"/> 的接口代理，代理目标为 <paramref name="implementationType"/>。
		/// </summary>
		/// <param name="serviceType">要注册的服务接口类型。</param>
		/// <param name="implementationType">实现类型，同时以自身类型注册为单例服务。</param>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>代理在首次解析时创建，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
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
		/// 使用单例生命周期注册 <typeparamref name="TService"/> 的接口代理，代理目标为给定实例。
		/// </summary>
		/// <param name="implementation">作为代理目标的实现实例。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>实例本身不会以其实现类型再次注册；代理在首次解析时创建，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
		public IServiceCollection AddProxiedSingleton<TService>(TService implementation)
		{
			services.AddSingleton(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), implementation!, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 使用单例生命周期注册 <typeparamref name="TService"/> 的接口代理，并应用指定的拦截器。
		/// </summary>
		/// <param name="interceptors">应用于代理的拦截器集合；为 <see langword="null"/> 时按空集合处理，不再从容器解析 <see cref="IInterceptor"/>。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为单例服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks><paramref name="interceptors"/> 在代理创建时才枚举。</remarks>
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
		/// 使用单例生命周期，将 <typeparamref name="TService" /> 指定的类型的代理服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="interceptorTypes">拦截器类型集合，创建代理时通过 <see cref="ActivatorUtilities.GetServiceOrCreateInstance(IServiceProvider, Type)"/> 获取或创建实例；为 <see langword="null"/> 时按空集合处理。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为单例服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
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
		/// 使用瞬态生命周期，将 <typeparamref name="TService" /> 指定的类型的代理服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为瞬态服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>每次解析都会创建新的代理实例，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
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
		/// 使用瞬态生命周期，将 <paramref name="serviceType" /> 指定的类型的代理服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="serviceType">要注册的服务接口类型。</param>
		/// <param name="implementationType">实现类型，同时以自身类型注册为瞬态服务。</param>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>每次解析都会创建新的代理实例，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
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
		/// 使用瞬态生命周期，将 <typeparamref name="TService" /> 指定的类型的代理服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="implementation">作为代理目标的实现实例。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>实例本身不会以其实现类型再次注册；每次解析都会基于该实例创建新的代理，拦截器取自容器中注册的全部 <see cref="IInterceptor"/>。</remarks>
		public IServiceCollection AddProxiedTransient<TService>(TService implementation)
		{
			services.AddTransient(typeof(TService), provider =>
			{
				var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
				var interceptors = provider.GetServices<IInterceptor>().ToArray();
				return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TService), implementation!, interceptors);
			});
			return services;
		}

		/// <summary>
		/// 使用瞬态生命周期，将 <typeparamref name="TService" /> 指定的类型的代理服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="interceptors">应用于代理的拦截器集合；为 <see langword="null"/> 时按空集合处理，不再从容器解析 <see cref="IInterceptor"/>。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为瞬态服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks><paramref name="interceptors"/> 在代理创建时才枚举。</remarks>
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
		/// 使用瞬态生命周期，将 <typeparamref name="TService" /> 指定的类型的代理服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="interceptorTypes">拦截器类型集合，创建代理时通过 <see cref="ActivatorUtilities.GetServiceOrCreateInstance(IServiceProvider, Type)"/> 获取或创建实例；为 <see langword="null"/> 时按空集合处理。</param>
		/// <typeparam name="TService">要注册的服务接口类型。</typeparam>
		/// <typeparam name="TImplementation">实现 <typeparamref name="TService"/> 的具体类型，同时以自身类型注册为瞬态服务。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
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
		/// 使用给定名称，将 <typeparamref name="TService" /> 指定的类型的瞬态服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="name">服务实现的名称。</param>
		/// <typeparam name="TService">服务类型。</typeparam>
		/// <typeparam name="TImplementation">实现类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <exception cref="InvalidOperationException">如果具有给定名称的 <typeparamref name="TService"/> 已注册，则抛出异常。</exception>
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
		/// 使用给定名称，将 <typeparamref name="TService" /> 指定的类型的作用域服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="name">服务实现的名称。</param>
		/// <typeparam name="TService">服务类型。</typeparam>
		/// <typeparam name="TImplementation">实现类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <exception cref="InvalidOperationException">如果具有给定名称的 <typeparamref name="TService"/> 已注册，则抛出异常。</exception>
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
		/// 使用给定名称，将 <typeparamref name="TService" /> 指定的类型的单例服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="name">服务实现的名称。</param>
		/// <typeparam name="TService">服务类型。</typeparam>
		/// <typeparam name="TImplementation">实现类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <exception cref="InvalidOperationException">如果具有给定名称的 <typeparamref name="TService"/> 已注册，则抛出异常。</exception>
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
		/// 使用工厂将 <typeparamref name="TService" /> 类型的服务添加到指定的 <see cref="IServiceCollection" />。
		/// </summary>
		/// <param name="factory">接收服务名称和 <see cref="IServiceProvider"/> 并返回对应服务实例的工厂。</param>
		/// <typeparam name="TService">服务类型。</typeparam>
		/// <returns>操作完成后对此实例的引用。</returns>
		/// <remarks>以瞬态方式注册 <see cref="NamedService{TService}"/> 委托；若该委托已存在，则保留先前的注册。</remarks>
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