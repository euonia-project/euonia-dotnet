using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The extension methods to register application services to <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
	private static int _factoryCount;

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

	/// <param name="services"></param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Register service context.
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		public void Register<TService>()
			where TService : class, IServiceContext, new()
		{
			var context = new TService();
			context.ConfigureServices(services);

			if (context.AutoRegisterPipelineBehaviors || context.AutoRegisterApplicationService)
			{
				var assembly = Assembly.GetAssembly(typeof(TService));
				var definedTypes = assembly!.DefinedTypes.ToArray();

				if (context.AutoRegisterApplicationService)
				{
					services.AddApplicationService(definedTypes);
				}

				if (context.AutoRegisterPipelineBehaviors)
				{
					services.AddPipelineBehaviors(definedTypes);
				}
			}

			services.TryAddSingleton<IServiceContext>(_ => context);
		}

		/// <summary>
		/// Register application service of module to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="assembly">The assembly which contains application services.</param>
		/// <returns></returns>
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
		/// Register pipeline behaviors of module to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="assembly">The assembly which contains pipeline behaviors.</param>
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
		/// Register application services of module to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="definedTypes">The application service types.</param>
		/// <returns></returns>
		/// <remarks>The application service type should inherit from <see cref="IApplicationService"/>.</remarks>
		private void AddApplicationService(TypeInfo[] definedTypes)
		{
			if (!definedTypes.Any())
			{
				return;
			}

			var types = definedTypes.Where(type => type.IsClass && !type.IsAbstract && typeof(IApplicationService).IsAssignableFrom(type));

			foreach (var implementationType in types)
			{
				// 注册为 Scoped：同一作用域（请求/消息）内，通过任意接口解析都共享同一个实现实例，
				// 避免同一实现类的多个接口视图各自创建独立实例、状态互相不可见。
				services.AddScoped(implementationType);

				// 仅注册业务接口；IDisposable、IHasLazyServiceProvider 等框架接口不创建代理。
				var interfaces = implementationType.GetInterfaces()
				                                   .Where(interfaceType => !_frameworkInterfaces.Contains(interfaceType))
				                                   .ToArray();

				if (interfaces.Length == 0)
				{
					continue;
				}

				foreach (var serviceType in interfaces)
				{
					services.TryAddScoped(serviceType, provider =>
					{
						var instance = provider.GetRequiredService(implementationType);
						Console.Error.WriteLine($"[DI] factory #{Interlocked.Increment(ref _factoryCount)}: serviceType={serviceType.Name} instanceType={instance.GetType().FullName} scope={provider.GetHashCode()}");
						if (instance is IHasLazyServiceProvider service)
						{
							var lazyServiceProvider = provider.GetService<ILazyServiceProvider>() ?? new LazyServiceProvider(provider);
							service.LazyServiceProvider = lazyServiceProvider;
						}

						var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
						var interceptors = provider.GetServices<IInterceptor>().ToArray();
						return proxyGenerator.CreateInterfaceProxyWithTarget(serviceType, instance, interceptors);
					});
				}
			}
		}

		/// <summary>
		/// Register pipeline behaviors to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="behaviorTypes"></param>
		/// <returns></returns>
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