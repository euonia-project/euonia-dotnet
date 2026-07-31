using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向 <see cref="IServiceCollection"/> 添加内存总线服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 向服务集合中添加内存总线服务。
		/// </summary>
		/// <param name="name">传输器名称。</param>
		/// <param name="configureOptions">用于配置 <see cref="InMemoryBusOptions"/> 的可选委托。</param>
		/// <returns>当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
		public IServiceCollection AddInMemoryBus(string name, Action<InMemoryBusOptions> configureOptions = null)
		{
			if (configureOptions != null)
			{
				services.Configure(configureOptions);
			}

			// 将内存队列消费者注册为瞬态服务。
			services.TryAddTransient<InMemoryConsumer>();

			// 将内存主题订阅者注册为瞬态服务。
			services.TryAddTransient<InMemorySubscriber>();
			
			// 将内存请求执行器注册为瞬态服务。
			services.TryAddTransient<InMemoryExecutor>();

			// 将内存传输器注册为单例服务。
			services.TryAddSingleton<InMemoryTransporter>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransporter>(name, (provider, _) => provider.GetRequiredService<InMemoryTransporter>());
			}

			// 将内存接收者注册器注册为瞬态服务，实现 IRecipientRegistrar 接口。
			if (!services.IsAddedImplementation<IRecipientRegistrar, InMemoryRecipientRegistrar>())
			{
				services.AddTransient<IRecipientRegistrar, InMemoryRecipientRegistrar>();
			}
			return services;
		}
	}
}