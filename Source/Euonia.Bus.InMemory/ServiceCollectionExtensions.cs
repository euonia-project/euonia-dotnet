using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

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

			// 每个容器独立的一对信使；传输器与接收者注册器必须共享同一对实例。
			services.TryAddSingleton<InMemoryMessengerProvider>();

			// 内存传输器是作用域服务：单播/请求需要读取调用方作用域以复用其服务。
			// 使用显式工厂，避免多个公开构造函数导致 DI 选中缺少访问器的重载。
			services.TryAddScoped<InMemoryTransporter>(provider => new InMemoryTransporter(
				provider.GetRequiredService<IOptions<InMemoryBusOptions>>(),
				provider.GetRequiredService<ILoggerFactory>(),
				provider.GetRequiredService<InMemoryMessengerProvider>(),
				provider.GetService<IServiceAccessor>(),
				provider.GetService<IRequestContextAccessor>()));

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedScoped<ITransporter>(name, (provider, _) => provider.GetRequiredService<InMemoryTransporter>());
			}

			// 将内存接收者注册器注册为单例服务，实现 IRecipientRegistrar 接口。
			// 单例生命周期确保被解析的接收者实例会被强引用持有，避免弱引用信使中的多播订阅者被提前回收。
			if (!services.IsAddedImplementation<IRecipientRegistrar, InMemoryRecipientRegistrar>())
			{
				services.AddSingleton<IRecipientRegistrar, InMemoryRecipientRegistrar>();
			}
			return services;
		}
	}
}