using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;
using Nerosoft.Euonia.Grpc;
using Nerosoft.Euonia.Modularity;

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
		public IServiceCollection AddGrpcBus(string name = "grpc", Action<GrpcBusOptions>? configureOptions = null)
		{
			services.AddOptions();
			// 保证消息总线核心与内置键控序列化器可用（重复调用安全）。
			services.AddEuoniaBus();

			// 自动绑定配置节 Euonia:Bus:Grpc，作为最低优先级的默认值；
			// 显式 services.Configure<GrpcBusOptions> 与该方法的 configure 形参始终优先，与调用先后无关。
			services.AddOptions<GrpcBusOptions>()
			        .BindConfigurationIfAvailable(GrpcBusOptions.SectionName);

			if (configureOptions != null)
			{
				services.Configure(configureOptions);
			}

			services.TryAddSingleton<GrpcTransporter>();

			if (!services.Any(descriptor => descriptor.ServiceType == typeof(ITransporter) && descriptor.ServiceKey is string key && key == name))
			{
				services.AddKeyedSingleton<ITransporter>(name, (provider, _) => provider.GetRequiredService<GrpcTransporter>());
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
			services.AddOptions();
			// 保证消息总线核心与内置键控序列化器可用（重复调用安全）。
			services.AddEuoniaBus();

			// 服务端同样需要配置节支持（例如 Euonia:Bus:Grpc:RequireAuthorization）。
			// 绑定以 Insert(0) 注册为最低优先级，因此与 AddGrpcBus 共存时不会覆盖客户端或宿主的显式配置。
			services.AddOptions<GrpcBusOptions>()
			        .BindConfigurationIfAvailable(GrpcBusOptions.SectionName);

			services.TryAddScoped<RemoteMessageService>();
			return services;
		}
	}
}

/// <summary>
/// gRPC 服务集合扩展。
/// </summary>
public static class GrpcServiceCollectionExtensions
{
	/// <summary>
	/// 将 gRPC 服务添加到指定的 <see cref="IServiceCollection"/>。
	/// </summary>
	/// <param name="services">要添加 gRPC 服务的服务集合。</param>
	/// <param name="configureOptions">用于进一步配置 <see cref="GrpcServiceOptions"/> 的可选委托；为 <see langword="null"/> 时使用默认设置。</param>
	/// <returns>原始服务集合，以支持链式调用。</returns>
	public static IServiceCollection AddGrpcService(this IServiceCollection services, Action<GrpcServiceOptions>? configureOptions = null)
	{
		services.AddGrpc(options =>
		{
			options.MaxReceiveMessageSize = null;
			options.EnableDetailedErrors = true;
			options.Interceptors.Add<ExceptionHandlingInterceptor>();
			options.Interceptors.Add<RequestTraceInterceptor>();
			configureOptions?.Invoke(options);
		});
		services.AddGrpcReflection();
		return services;
	}
}