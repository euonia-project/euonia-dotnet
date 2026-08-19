using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 提供将应用服务与管道行为注册到 <see cref="IServiceCollection"/> 的扩展方法。
/// </summary>
/// <remarks>
/// 应用服务注册采用「按类型持有者 + Castle 动态代理」的机制：
/// 同一作用域内，接口代理与实现类代理共享同一个目标实例，
/// 从而保证通过接口或实现类型解析时，拦截器（如授权、校验、日志）始终生效。
/// </remarks>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 不需要注册为服务契约的框架接口，避免为它们创建无意义的代理。
	/// </summary>
	private static readonly HashSet<Type> _frameworkInterfaces = new()
	{
		typeof(IDisposable),
		typeof(IAsyncDisposable),
		typeof(IHasLazyServiceProvider),
		typeof(IApplicationService),
	};

	/// <summary>
	/// 原始目标实例的按类型持有者，用于让接口代理与实现类代理共享同一个作用域实例。
	/// </summary>
	private interface IApplicationServiceTarget
	{
		/// <summary>
		/// 获取被代理的目标实例。
		/// </summary>
		object Instance { get; }
	}

	/// <summary>
	/// <see cref="IApplicationServiceTarget"/> 的强类型实现，按目标实例类型保存实例。
	/// </summary>
	/// <typeparam name="T">被代理的目标实例类型。</typeparam>
	private sealed class ApplicationServiceTarget<T> : IApplicationServiceTarget
		where T : class
	{
		/// <summary>
		/// 使用指定的目标实例初始化 <see cref="ApplicationServiceTarget{T}"/>。
		/// </summary>
		/// <param name="instance">被代理的目标实例。</param>
		public ApplicationServiceTarget(T instance)
		{
			Instance = instance;
		}

		/// <inheritdoc/>
		public object Instance { get; }
	}

	/// <summary>
	/// 为 <see cref="IServiceCollection"/> 提供应用服务与管道行为的注册扩展。
	/// </summary>
	/// <param name="services">要注册服务的服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 注册服务上下文，并按上下文的配置自动扫描注册应用服务与管道行为。
		/// </summary>
		/// <typeparam name="TService">服务上下文类型，需实现 <see cref="IServiceContext"/> 并提供无参构造函数。</typeparam>
		/// <remarks>
		/// 上下文默认以单例方式注册到服务集合；
		/// 当 <see cref="IServiceContext.AutoRegisterApplicationService"/> 或 <see cref="IServiceContext.AutoRegisterPipelineBehaviors"/> 为 <c>true</c> 时，
		/// 会扫描 <see cref="IServiceContext.Assembly"/> 指向的程序集并自动注册相应的服务。
		/// </remarks>
		public void Register<TService>()
			where TService : class, IServiceContext, new()
		{
			var context = new TService();
			context.ConfigureServices(services);

			if (context.AutoRegisterPipelineBehaviors || context.AutoRegisterApplicationService)
			{
				// 使用上下文的 Assembly（默认为上下文类型所在程序集，可覆写指向其他程序集），
				// 扫描并注册应用服务与管道行为。
				var assembly = context.Assembly;
				if (assembly != null)
				{
					var definedTypes = assembly.DefinedTypes.ToArray();

					if (context.AutoRegisterApplicationService)
					{
						services.AddApplicationService(definedTypes);
					}

					if (context.AutoRegisterPipelineBehaviors)
					{
						services.AddPipelineBehaviors(definedTypes);
					}
				}
			}

			services.TryAddSingleton<IServiceContext>(_ => context);
		}

		/// <summary>
		/// 扫描指定程序集中的应用服务并注册到 <see cref="IServiceCollection"/>。
		/// </summary>
		/// <param name="assembly">包含应用服务的程序集；为 <c>null</c> 时直接返回。</param>
		/// <remarks>
		/// 仅注册继承自 <see cref="IApplicationService"/> 的非抽象类。
		/// </remarks>
		public void AddApplicationService(Assembly assembly)
		{
			if (assembly == null)
			{
				return;
			}

			var definedTypes = AssemblyHelper.GetDefinedTypes(assembly)
			                                 .ToArray();

			services.AddApplicationService(definedTypes);
		}

		/// <summary>
		/// 扫描指定程序集中的管道行为并注册到 <see cref="IServiceCollection"/>。
		/// </summary>
		/// <param name="assembly">包含管道行为的程序集；为 <c>null</c> 时直接返回。</param>
		/// <remarks>
		/// 仅注册实现 <c>IPipelineBehavior&lt;TRequest, TResponse&gt;</c> 的非泛型行为类型。
		/// </remarks>
		public void AddPipelineBehaviors(Assembly assembly)
		{
			if (assembly == null)
			{
				return;
			}

			var definedTypes = assembly.DefinedTypes.ToArray();
			services.AddPipelineBehaviors(definedTypes);
		}

		/// <summary>
		/// 将给定类型中继承自 <see cref="IApplicationService"/> 的非抽象类注册为应用服务。
		/// </summary>
		/// <param name="definedTypes">待扫描的类型集合。</param>
		/// <remarks>
		/// 每个实现类型都会注册：原始实例持有者（Scoped）、实现类代理（Scoped）以及全部业务接口代理（Scoped）。
		/// 业务接口为排除 <see cref="_frameworkInterfaces"/> 之后由实现类公开的接口；
		/// 接口代理与类代理在同一个作用域内共享同一个目标实例。
		/// </remarks>
		private void AddApplicationService(TypeInfo[] definedTypes)
		{
			if (!definedTypes.Any())
			{
				return;
			}

			var types = definedTypes.Where(type => type.IsClass && !type.IsAbstract && typeof(IApplicationService).IsAssignableFrom(type));

			foreach (var implementationType in types)
			{
				// 原始实例的按类型持有者（Scoped）：同一作用域（请求/消息）内，
				// 接口代理与实现类代理共享同一个目标实例，避免状态分裂。
				var holderType = typeof(ApplicationServiceTarget<>).MakeGenericType(implementationType);
				services.AddScoped(holderType, provider =>
				{
					var instance = ActivatorUtilities.CreateInstance(provider, implementationType);
					return Activator.CreateInstance(holderType, instance);
				});

				// 实现类直接解析也走代理（类代理），避免绕过拦截器；
				// 仅注册业务接口；IDisposable、IHasLazyServiceProvider 等框架接口不创建代理。
				var interfaces = implementationType.GetInterfaces()
				                                   .Where(interfaceType => !_frameworkInterfaces.Contains(interfaceType))
				                                   .ToArray();

				services.AddScoped(implementationType, provider => CreateImplementationProxy(provider, implementationType, holderType));

				if (interfaces.Length == 0)
				{
					continue;
				}

				foreach (var serviceType in interfaces)
				{
					services.TryAddScoped(serviceType, provider => CreateInterfaceProxy(provider, holderType, serviceType));
				}
			}
		}

		/// <summary>
		/// 创建实现类的类代理，使直接解析实现类型时拦截器同样生效。
		/// </summary>
		/// <param name="provider">用于解析目标实例持有者、拦截器等服务的服务提供程序。</param>
		/// <param name="implementationType">应用服务的实现类型。</param>
		/// <param name="holderType">该实现类型对应的目标实例持有者类型。</param>
		/// <returns>类代理实例；无法创建代理时返回原始实例。</returns>
		/// <remarks>
		/// Castle 的类代理通过生成子类覆写目标方法实现拦截，因此仅对 <c>virtual</c> 成员生效，
		/// 且要求目标类型存在默认构造函数（生成的代理子类需调用 <c>base()</c>）；
		/// 不满足条件时回退为裸实例（接口路径的拦截不受影响）。
		/// </remarks>
		private static object CreateImplementationProxy(IServiceProvider provider, Type implementationType, Type holderType)
		{
			var holder = (IApplicationServiceTarget)provider.GetRequiredService(holderType);
			var instance = holder.Instance;

			if (instance is IHasLazyServiceProvider service)
			{
				var lazyServiceProvider = provider.GetService<ILazyServiceProvider>() ?? new LazyServiceProvider(provider);
				service.LazyServiceProvider = lazyServiceProvider;
			}

			if (implementationType.GetConstructor(Type.EmptyTypes) == null)
			{
				return instance;
			}

			var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
			var interceptors = provider.GetServices<IInterceptor>().ToArray();
			return proxyGenerator.CreateClassProxyWithTarget(implementationType, instance, interceptors);
		}

		/// <summary>
		/// 创建接口代理，拦截接口上声明的全部方法。
		/// </summary>
		/// <param name="provider">用于解析目标实例持有者、拦截器等服务的服务提供程序。</param>
		/// <param name="holderType">该实现类型对应的目标实例持有者类型。</param>
		/// <param name="serviceType">需要代理的服务接口类型。</param>
		/// <returns>接口代理实例。</returns>
		private static object CreateInterfaceProxy(IServiceProvider provider, Type holderType, Type serviceType)
		{
			var holder = (IApplicationServiceTarget)provider.GetRequiredService(holderType);
			var instance = holder.Instance;

			if (instance is IHasLazyServiceProvider service)
			{
				var lazyServiceProvider = provider.GetService<ILazyServiceProvider>() ?? new LazyServiceProvider(provider);
				service.LazyServiceProvider = lazyServiceProvider;
			}

			var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
			var interceptors = provider.GetServices<IInterceptor>().ToArray();
			return proxyGenerator.CreateInterfaceProxyWithTarget(serviceType, instance, interceptors);
		}

		/// <summary>
		/// 将给定类型中实现 <c>IPipelineBehavior&lt;TRequest, TResponse&gt;</c> 的非泛型行为注册到 <see cref="IServiceCollection"/>。
		/// </summary>
		/// <param name="behaviorTypes">待扫描的行为类型集合。</param>
		/// <remarks>
		/// 行为以瞬态方式注册到其实现的 <c>IPipelineBehavior&lt;TRequest, TResponse&gt;</c> 接口上。
		/// </remarks>
		private void AddPipelineBehaviors(TypeInfo[] behaviorTypes)
		{
			foreach (var behaviorType in behaviorTypes)
			{
				var interfaces = behaviorType.GetInterfaces()
				                             .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
				                             .ToList();
				foreach (var @interface in interfaces)
				{
					if (behaviorType.IsGenericType)
					{
						continue;
					}

					services.AddTransient(@interface, behaviorType);
				}
			}
		}
	}
}