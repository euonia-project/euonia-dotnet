using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Nerosoft.Euonia.Telemetry;

namespace Nerosoft.Euonia.Telemetry.Otel;

/// <summary>
/// Euonia 遥测的 OpenTelemetry 接入扩展。
/// </summary>
public static class EuoniaOpenTelemetryExtensions
{
	/// <summary>
	/// 注册 Euonia 遥测并接入 OpenTelemetry；重复调用幂等。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configureTelemetry">遥测选项配置委托。</param>
	/// <param name="configure">OTel 构建器配置委托，用于添加导出器与宿主 instrumentation。</param>
	/// <returns>服务集合。</returns>
	/// <remarks>
	/// 本方法不替宿主引入 instrumentation 包；<c>AddAspNetCoreInstrumentation</c> 等由宿主自行引用。
	/// Resource 在注册期读取配置与委托；运行期热更新只影响埋点开关，不影响 Resource。
	/// </remarks>
	public static IServiceCollection AddEuoniaOpenTelemetry(
		this IServiceCollection services,
		Action<TelemetryOptions>? configureTelemetry = null,
		Action<OpenTelemetryBuilder>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddEuoniaTelemetry(configureTelemetry);

		var options = ResolveOptions(services, configureTelemetry);

		var builder = services.AddOpenTelemetry()
		                      .ConfigureResource(resource => ApplyResource(resource, options))
		                      .WithTracing(tracing => tracing.AddEuoniaSources())
		                      .WithMetrics(metrics => metrics.AddEuoniaSources())
		                      .WithLogging();

		// Scope 带 TraceId / SpanId / TenantId，日志可与 Trace 对齐。
		services.Configure<OpenTelemetryLoggerOptions>(logging =>
		{
			logging.IncludeScopes = true;
			logging.IncludeFormattedMessage = true;
		});

		configure?.Invoke(builder);
		return services;
	}

	private static TelemetryOptions ResolveOptions(IServiceCollection services, Action<TelemetryOptions>? configure)
	{
		var options = new TelemetryOptions();

		var configuration = services.FirstOrDefault(x => x.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration))
		                          ?.ImplementationInstance as Microsoft.Extensions.Configuration.IConfiguration;

		var section = configuration?.GetSection(TelemetryOptions.ConfigurationSection);
		if (section != null)
		{
			Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(section, options);
		}

		configure?.Invoke(options);
		return options;
	}

	/// <summary>
	/// 把 Euonia 默认源目录加入跟踪构建器。
	/// </summary>
	/// <param name="builder">跟踪构建器。</param>
	/// <param name="registry">可选的运行时源注册表。</param>
	/// <returns>跟踪构建器。</returns>
	public static TracerProviderBuilder AddEuoniaSources(this TracerProviderBuilder builder, TelemetrySourceRegistry? registry = null)
	{
		ArgumentNullException.ThrowIfNull(builder);

		foreach (var source in EuoniaSources.Default)
		{
			builder.AddSource(source);
		}

		if (registry != null)
		{
			foreach (var source in registry.GetAll())
			{
				builder.AddSource(source.SourceName);
			}
		}

		return builder;
	}

	/// <summary>
	/// 把 Euonia 默认源目录加入指标构建器。
	/// </summary>
	/// <param name="builder">指标构建器。</param>
	/// <param name="registry">可选的运行时源注册表。</param>
	/// <returns>指标构建器。</returns>
	public static MeterProviderBuilder AddEuoniaSources(this MeterProviderBuilder builder, TelemetrySourceRegistry? registry = null)
	{
		ArgumentNullException.ThrowIfNull(builder);

		foreach (var source in EuoniaSources.Default)
		{
			builder.AddMeter(source);
		}

		if (registry != null)
		{
			foreach (var source in registry.GetAll())
			{
				builder.AddMeter(source.MeterName);
			}
		}

		return builder;
	}

	/// <summary>
	/// 设置 W3C 传播器（TraceContext + Baggage）。
	/// </summary>
	/// <remarks>宿主已自行配置传播器时不要调用；本方法覆盖全局默认值。</remarks>
	public static void UseEuoniaPropagator()
	{
		Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
		[
			new TraceContextPropagator(),
			new BaggagePropagator(),
		]));
	}

	private static void ApplyResource(ResourceBuilder resource, TelemetryOptions options)
	{
		var attributes = new List<KeyValuePair<string, object>>();

		if (!string.IsNullOrWhiteSpace(options.ServiceName))
		{
			attributes.Add(new KeyValuePair<string, object>("service.name", options.ServiceName));
		}

		if (!string.IsNullOrWhiteSpace(options.ServiceVersion))
		{
			attributes.Add(new KeyValuePair<string, object>("service.version", options.ServiceVersion));
		}

		if (!string.IsNullOrWhiteSpace(options.Environment))
		{
			attributes.Add(new KeyValuePair<string, object>("deployment.environment", options.Environment));
		}

		attributes.Add(new KeyValuePair<string, object>("euonia.version", typeof(TelemetryOptions).Assembly.GetName().Version?.ToString() ?? "unknown"));

		resource.AddAttributes(attributes);
	}
}
