using Apache.NMS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// <see cref="IServiceCollection"/> 的扩展实例，用于添加 ActiveMQ 总线相关服务。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">要添加服务的目标服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 将 ActiveMQ 总线服务添加到服务集合中。
		/// 注册连接工厂、持久连接、传输器、消费者、订阅者、执行器及接收器注册器等全部 ActiveMQ 相关服务。
		/// </summary>
		/// <param name="name">传输器的名称，用作键控服务注册的键。</param>
		/// <param name="configureOptions">用于配置 ActiveMQ 总线选项的操作。</param>
		/// <returns>返回已添加服务的 <see cref="IServiceCollection"/> 实例。</returns>
		/// <exception cref="InvalidOperationException">当 <see cref="ActiveMqBusOptions"/> 未正确配置时抛出。</exception>
		public IServiceCollection AddActiveMqBus(string name, Action<ActiveMqBusOptions> configureOptions = null)
		{
			if (configureOptions != null)
			{
				services.Configure(configureOptions);
			}

			// 注册单例 IConnectionFactory 实现，使用已配置的 ActiveMQ 选项。
			services.TryAddSingleton<IConnectionFactory>(provider =>
			{
				var options = provider.GetService<IOptions<ActiveMqBusOptions>>()?.Value;

				if (options == null)
				{
					throw new InvalidOperationException("ActiveMqBusOptions was not configured.");
				}

				// 使用提供的连接 URI 创建并返回 ActiveMQ 连接工厂。
				var factory = new NMSConnectionFactory(options.Connection);
				return factory;
			});
			
			// 注册单例 IPersistentConnection 实现。
			services.TryAddSingleton<IPersistentConnection, DefaultPersistentConnection>();

			// 注册 ActiveMQ 传输相关服务。
			services.TryAddTransient<ActiveMqConsumer>();
			services.TryAddTransient<ActiveMqSubscriber>();
			services.TryAddTransient<ActiveMqExecutor>();
			services.TryAddSingleton<ActiveMqTransporter>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransporter>(name, (provider, _) => provider.GetService<ActiveMqTransporter>());
			}

			if (!services.IsAddedImplementation<IRecipientRegistrar, ActiveMqRecipientRegistrar>())
			{
				services.AddTransient<IRecipientRegistrar, ActiveMqRecipientRegistrar>();
			}
			
			// 在此处添加 ActiveMQ 总线相关服务
			return services;
		}
	}
}