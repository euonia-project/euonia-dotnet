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
		/// 总线（<see cref="IBus"/>）、分发器（<see cref="IDispatcher"/>）以及接收器激活后台服务（<see cref="RecipientActivator"/>）。
		/// 同时自动注册所有已通过 <see cref="ChannelRegistrar"/> 注册的处理器类型，并启用管道支持。
		/// </summary>
		/// <param name="config">用于配置消息总线（<see cref="DefaultConfigurator"/>）的可选委托。</param>
		/// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		public IServiceCollection AddEuoniaBus(Action<DefaultConfigurator> config = null)
		{
			// 注册单例 IConfigurator，通过配置器实例执行用户配置委托
			var configurator = Singleton<DefaultConfigurator>.Get(() => new DefaultConfigurator());
			config?.Invoke(configurator);
			services.AddSingleton<IConfigurator>(configurator);

			// 启用管道（Pipeline）支持
			services.AddPipeline();

			// 注册单例 IHandlerContext，遍历所有已注册的通道和处理器，
			// 根据处理器方法返回类型（Task&lt;T&gt;）判断是否为请求-响应处理器，
			// 并分别通过泛型 Register 方法或普通 Register 方法进行注册
			services.TryAddSingleton<IHandlerContext>(provider =>
			{
				var configurator = provider.GetRequiredService<IConfigurator>();
				var context = new DefaultHandlerContext(provider);

				var registerMethod = typeof(DefaultHandlerContext).GetMethod(nameof(DefaultHandlerContext.Register), 3, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, [typeof(string)]);

				foreach (var (channel, registration) in configurator.Registrations)
				{
					foreach (var handler in registration.Handlers)
					{
						if (handler.HandlerType.IsInterface && handler.HandlerType.GetGenericTypeDefinition() == typeof(IHandler<,>))
						{
							var resultType = handler.HandlerType.GenericTypeArguments[1];
							registerMethod?.MakeGenericMethod(registration.MessageType, resultType, handler.HandlerType).Invoke(context, [channel]);
						}
						else
						{
							context.Register(channel, handler);
						}
					}
				}

				return context;
			});
			services.TryAddSingleton<IBus, MessageBus>();
			services.TryAddSingleton<IDispatcher, StrategicDispatcher>();
			services.AddHostedService<RecipientActivator>();

			return services;
		}

		public IServiceCollection AddMessageHandler(ServiceLifetime lifetime, params Assembly[] assemblies)
		{
			if (assemblies == null || assemblies.Length == 0)
			{
				throw new ArgumentNullException(nameof(assemblies), "Assemblies cannot be null or empty.");
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

		public IServiceCollection AddMessageHandler(ServiceLifetime lifetime, params Type[] handlerTypes)
		{
			if (handlerTypes == null || handlerTypes.Length == 0)
			{
				throw new ArgumentNullException(nameof(handlerTypes), "Handler types cannot be null or empty.");
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