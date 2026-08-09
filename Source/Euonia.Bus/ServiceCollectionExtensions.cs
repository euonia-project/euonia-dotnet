using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向服务集合添加消息总线的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">用于注册服务的 <see cref="IServiceCollection"/> 实例。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 向服务集合添加消息总线。
		/// 注册消息总线核心服务，包括配置器（<see cref="IConfigurator"/>）、处理器上下文（<see cref="IHandlerContext"/>）、
		/// 总线（<see cref="IBus"/>）、分发器（<see cref="IDispatcher"/>）以及接收器激活后台服务（<see cref="ServiceActivator"/>），
		/// 并启用管道支持。
		/// </summary>
		/// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		public IServiceCollection AddEuoniaBus()
		{
			// 注册单例 IConfigurator，通过配置器实例执行用户配置委托
			services.TryAddActivatedSingleton<IConfigurator, DefaultConfigurator>();

			// 启用管道（Pipeline）支持
			services.AddPipeline();
			services.TryAddActivatedSingleton<IHandlerContext, DefaultHandlerContext>();
			services.TryAddSingleton<IBus, MessageBus>();
			services.TryAddSingleton<IDispatcher, StrategicDispatcher>();
			services.AddHostedService<ServiceActivator>();

			return services;
		}

		/// <summary>
		/// 注册一个用于配置消息总线的 <see cref="ConfiguratorBuilder"/> 委托。
		/// 配置委托将在消息总线启动前被执行，用于设置消息约定、传输策略和处理器注册。
		/// </summary>
		/// <param name="configure">用于配置 <see cref="IConfigurator"/> 的委托，不能为 <c>null</c>。</param>
		/// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="configure"/> 为 <c>null</c> 时抛出。</exception>
		public IServiceCollection AddConfiguratorBuilder(Action<IConfigurator> configure)
		{
			if (configure == null)
			{
				throw new ArgumentNullException(nameof(configure), @"Configurator builder cannot be null.");
			}

			services.TryAddSingleton<ConfiguratorBuilder>(_ =>
			{
				void Build(IConfigurator configurator) => configure(configurator);
				return Build;
			});
			return services;
		}

		/// <summary>
		/// 扫描指定程序集，将其中实现了 <see cref="IHandler{TMessage, TResult}"/> 接口的消息处理器
		/// 注册到服务集合中。
		/// </summary>
		/// <param name="lifetime">处理器服务的生命周期。</param>
		/// <param name="assemblies">要扫描的程序集数组。</param>
		/// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="assemblies"/> 为 <c>null</c> 或空时抛出。</exception>
		public IServiceCollection AddMessageHandler(ServiceLifetime lifetime, params Assembly[] assemblies)
		{
			if (assemblies == null || assemblies.Length == 0)
			{
				throw new ArgumentNullException(nameof(assemblies), @"Assemblies cannot be null or empty.");
			}

			var handlerTypes = assemblies.SelectMany(t => t.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<,>))));

			foreach (var handlerType in handlerTypes)
			{
				var interfaces = handlerType.GetInterfaces()
				                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandler<,>))
				                            .ToList();

				foreach (var @interface in interfaces)
				{
					services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
				}
			}

			return services;
		}

		/// <summary>
		/// 将指定的泛型处理器类型中实现 <see cref="IHandler{TMessage, TResult}"/> 的接口注册到服务集合中。
		/// </summary>
		/// <typeparam name="THandler">要实现 <see cref="IHandler{TMessage, TResult}"/> 接口的处理器类型。</typeparam>
		/// <param name="lifetime">处理器服务的生命周期。</param>
		/// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		/// <exception cref="ArgumentException">当 <typeparamref name="THandler"/> 未实现任何 <see cref="IHandler{TMessage, TResult}"/> 接口时抛出。</exception>
		public IServiceCollection AddMessageHandler<THandler>(ServiceLifetime lifetime)
			where THandler : class
		{
			var handlerType = typeof(THandler);
			var interfaces = handlerType.GetInterfaces()
			                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandler<,>))
			                            .ToList();
			if (!interfaces.Any())
			{
				throw new ArgumentException($"The type {handlerType.FullName} does not implement any IHandler<,> interface.");
			}

			foreach (var @interface in interfaces)
			{
				services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
			}

			return services;
		}

		/// <summary>
		/// 将指定的处理器类型中实现 <see cref="IHandler{TMessage, TResult}"/> 的接口注册到服务集合中。
		/// </summary>
		/// <param name="lifetime">处理器服务的生命周期。</param>
		/// <param name="handlerTypes">要注册的处理器类型数组。</param>
		/// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="handlerTypes"/> 为 <c>null</c> 或空时抛出。</exception>
		/// <exception cref="ArgumentException">当某个处理器类型不是非抽象类，或未实现任何 <see cref="IHandler{TMessage, TResult}"/> 接口时抛出。</exception>
		public IServiceCollection AddMessageHandler(ServiceLifetime lifetime, params Type[] handlerTypes)
		{
			if (handlerTypes == null || handlerTypes.Length == 0)
			{
				throw new ArgumentNullException(nameof(handlerTypes), @"Handler types cannot be null or empty.");
			}

			foreach (var handlerType in handlerTypes)
			{
				if (handlerType.IsPrimitive || !handlerType.IsClass || handlerType.IsInterface || handlerType.IsAbstract)
				{
					throw new ArgumentException($"The type {handlerType.FullName} must be a non-abstract class.");
				}

				var interfaces = handlerType.GetInterfaces()
				                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandler<,>))
				                            .ToList();
				if (!interfaces.Any())
				{
					throw new ArgumentException($"The type {handlerType.FullName} does not implement any IHandler<,> interface.");
				}

				foreach (var @interface in interfaces)
				{
					services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
				}
			}

			return services;
		}
	}
}