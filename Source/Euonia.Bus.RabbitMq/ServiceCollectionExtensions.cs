using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.RabbitMq;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供用于添加 RabbitMQ 总线服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// <see cref="IServiceCollection"/> 的扩展实例，用于添加 RabbitMQ 总线相关服务。
	/// </summary>
	/// <param name="services">要添加服务的目标服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 将 RabbitMQ 总线服务添加到服务集合中。
		/// 注册连接工厂、持久连接、传输器、消费者、订阅者、执行器及接收器注册器等全部 RabbitMQ 相关服务。
		/// </summary>
		/// <param name="name">传输器的名称，用作键控服务注册的键。</param>
		/// <param name="configureOptions">用于配置 <see cref="RabbitMqBusOptions"/> 的可选委托。</param>
		/// <returns>原始服务集合，以支持链式调用。</returns>
		/// <exception cref="InvalidOperationException">当 <see cref="RabbitMqBusOptions"/> 未正确配置时抛出。</exception>
		public IServiceCollection AddRabbitMqBus(string name, Action<RabbitMqBusOptions> configureOptions = null)
		{
			if (configureOptions != null)
			{
				services.Configure(configureOptions);
			}

			// 注册单例 IConnectionFactory 实现，使用已配置的 RabbitMQ 选项。
			services.TryAddSingleton<IConnectionFactory>(provider =>
			{
				var options = provider.GetService<IOptions<RabbitMqBusOptions>>()?.Value;

				if (options == null)
				{
					throw new InvalidOperationException("RabbitMqMessageBusOptions was not configured.");
				}

				// 使用提供的连接 URI 创建并返回 RabbitMQ 连接工厂。
				var factory = new ConnectionFactory { Uri = new Uri(options.Connection) };
				return factory;
			});

			// 注册单例 IPersistentConnection 实现。
			services.TryAddSingleton<IPersistentConnection, DefaultPersistentConnection>();

			// 注册 RabbitMQ 传输相关服务。
			services.TryAddTransient<RabbitMqConsumer>();
			services.TryAddTransient<RabbitMqSubscriber>();
			services.TryAddTransient<RabbitMqExecutor>();
			services.TryAddSingleton<RabbitMqTransporter>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransporter>(name, (provider, _) => provider.GetService<RabbitMqTransporter>());
			}

			if (!services.IsAddedImplementation<IRecipientRegistrar, RabbitMqRecipientRegistrar>())
			{
				services.AddTransient<IRecipientRegistrar, RabbitMqRecipientRegistrar>();
			}

			return services;
		}
	}
}