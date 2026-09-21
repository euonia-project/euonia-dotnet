using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供用于添加 gRPC 总线服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// <see cref="IServiceCollection"/> 的扩展实例，用于添加 gRPC 总线相关服务。
	/// </summary>
	/// <param name="services">要添加服务的目标服务集合。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 将基于 gRPC 的 <see cref="ITransporter"/> 传输器注册到服务集合中。
		/// 该传输器仅支持请求-响应（<c>CallAsync</c>）调用。
		/// </summary>
		/// <param name="name">传输器的名称，用作键控服务注册的键。</param>
		/// <param name="configureOptions">用于配置 <see cref="GrpcBusOptions"/> 的可选委托。</param>
		/// <returns>原始服务集合，以支持链式调用。</returns>
		public IServiceCollection AddGrpcBus(string name = "grpc", Action<GrpcBusOptions> configureOptions = null)
		{
			services.AddOptions();
			if (configureOptions != null)
			{
				services.Configure(configureOptions);
			}

			services.TryAddSingleton<GrpcTransporter>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransporter>(name, (provider, _) => provider.GetService<GrpcTransporter>());
			}

			return services;
		}

		/// <summary>
		/// 注册 gRPC 远程消息服务端（<see cref="RemoteMessageService"/>）。
		/// 随后需调用 <c>MapGrpcBusService</c> 将服务映射到端点。
		/// </summary>
		/// <returns>原始服务集合，以支持链式调用。</returns>
		public IServiceCollection AddGrpcBusServer()
		{
			services.TryAddSingleton<RemoteMessageService>();
			return services;
		}
	}
}