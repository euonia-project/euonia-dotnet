using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 遥测能力的服务注册扩展。
/// </summary>
public static class TelemetryServiceCollectionExtensions
{
	/// <summary>
	/// 注册遥测核心服务。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">遥测选项配置委托。</param>
	/// <returns>注册完成后的服务集合。</returns>
	/// <remarks>
	/// 本方法不引入 OTel SDK；导出由 <c>Euonia.Telemetry.Otel</c> 或宿主自行配置。
	/// </remarks>
	public static IServiceCollection AddEuoniaTelemetry(this IServiceCollection services, Action<TelemetryOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		var builder = services.AddOptions<TelemetryOptions>();
		if (services.Any(x => x.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
		{
			builder.BindConfiguration(TelemetryOptions.ConfigurationSection);
		}

		if (configure != null)
		{
			builder.Configure(configure);
		}

		services.TryAddSingleton<ITelemetryRedactor, DefaultTelemetryRedactor>();
		services.TryAddSingleton<ITelemetryPropagator, DefaultTelemetryPropagator>();
		services.TryAddSingleton<TelemetrySourceRegistry>();
		services.TryAddSingleton<ITelemetryContextProvider>(provider => new DefaultTelemetryContextProvider(
			provider.GetService<IExecutionContextAccessor>()));
		services.TryAddEnumerable(ServiceDescriptor.Singleton<ITelemetryEnricher, TelemetryContextEnricher>());
		services.TryAddSingleton<ITelemetrySourceFactory, TelemetrySourceFactory>();

		return services;
	}
}
