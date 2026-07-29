using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The extension methods to register application services to <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
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
				services.AddTransient(implementationType);

				var interfaces = implementationType.GetInterfaces();

				if (interfaces.Length == 0)
				{
					continue;
				}

				foreach (var serviceType in interfaces)
				{
					services.TryAddTransient(serviceType, provider =>
					{
						var instance = provider.GetRequiredService(implementationType);
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