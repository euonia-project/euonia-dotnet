using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
	/// <param name="configuration">可选的配置实例；以工厂方式注册 IConfiguration 时需显式传入。</param>
	/// <param name="configurePropagator">是否设置进程级 Euonia 传播器（默认不覆盖宿主配置）。</param>
	/// <returns>服务集合。</returns>
	/// <remarks>
	/// 本方法不替宿主引入 instrumentation 包；<c>AddAspNetCoreInstrumentation</c> 等由宿主自行引用。
	/// Resource 在注册期读取配置与委托；运行期热更新只影响埋点开关，不影响 Resource。
	/// </remarks>
	public static IServiceCollection AddEuoniaOpenTelemetry(
		this IServiceCollection services,
		Action<TelemetryOptions>? configureTelemetry = null,
		Action<OpenTelemetryBuilder>? configure = null,
		Microsoft.Extensions.Configuration.IConfiguration? configuration = null,
		bool configurePropagator = false)
	{
		ArgumentNullException.ThrowIfNull(services);

		// 幂等：重复调用不应重复注册构建器/导出器。
		if (services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(EuoniaOpenTelemetryRegistration))?.ImplementationInstance is EuoniaOpenTelemetryRegistration existing)
		{
			// 重复调用不重复建 Provider/导出器，但第二次的选项与构建器配置仍然生效。
			if (configureTelemetry != null)
			{
				services.Configure(configureTelemetry);
			}

			if (configure != null && existing.Builder != null)
			{
				configure(existing.Builder);
			}

			return services;
		}

		var registration = new EuoniaOpenTelemetryRegistration();
		services.AddSingleton(registration);

		// 不构造临时容器：Resource 需要一份启动期快照，而 DI 侧仍由 Options 绑定。
		// configureTelemetry 只在快照上执行一次，DI 侧注册的是快照复制动作，避免委托执行两次。
		var defaults = new TelemetryOptions();
		var options = new TelemetryOptions();
		// 工厂注册的 IConfiguration 无法在注册期读取；优先使用宿主显式传入的实例。
		configuration ??= services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration))
		                          ?.ImplementationInstance as Microsoft.Extensions.Configuration.IConfiguration;
		if (configuration != null)
		{
			Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(configuration.GetSection(TelemetryOptions.ConfigurationSection), options);
		}
		configureTelemetry?.Invoke(options);

		var changed = typeof(TelemetryOptions).GetProperties()
		                                      .Where(property => property.CanWrite && property.CanRead)
		                                      .Where(property => property.PropertyType != typeof(TelemetryRedactionOptions)
		                                                         && property.PropertyType != typeof(Dictionary<string, TelemetryComponentOptions>))
		                                      .Where(property => !Equals(property.GetValue(defaults), property.GetValue(options)))
		                                      .ToArray();

		// Provider 订阅与工厂登记必须使用同一个注册表实例，兼容先 AddEuoniaTelemetry 的顺序。
		var registry = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(TelemetrySourceRegistry))
		                      ?.ImplementationInstance as TelemetrySourceRegistry
		               ?? new TelemetrySourceRegistry();
		services.RemoveAll<TelemetrySourceRegistry>();
		services.AddSingleton(registry);
		services.AddEuoniaTelemetry();
		services.Configure<TelemetryOptions>(target =>
		{
			foreach (var property in changed)
			{
				property.SetValue(target, property.GetValue(options));
			}

			// 引用类型按“合并”而不是整体覆盖，避免清空宿主已有的组件开关与脱敏键。
			foreach (var component in options.Components)
			{
				target.Components[component.Key] = component.Value;
			}

			if (options.Redaction.Enabled != defaults.Redaction.Enabled)
			{
				target.Redaction.Enabled = options.Redaction.Enabled;
			}

			// 与宿主已有键取并集：既不覆盖宿主自定义键，也保留默认键。
			target.Redaction.Keys = target.Redaction.Keys
			                              .Union(options.Redaction.Keys, StringComparer.OrdinalIgnoreCase)
			                              .ToArray();
		});

		var builder = services.AddOpenTelemetry()
		                      .ConfigureResource(resource => ApplyResource(resource, options))
		                      .WithLogging();

		// 启动期开关决定是否构建对应管道；关闭时不订阅任何 Euonia 源。
		if (options.Enabled && options.TracesEnabled)
		{
			builder.WithTracing(tracing => tracing.AddEuoniaSources(registry));
		}

		if (options.Enabled && options.MetricsEnabled)
		{
			builder.WithMetrics(metrics => metrics.AddEuoniaSources(registry));
		}

		// Scope 带 TraceId / SpanId / TenantId，日志可与 Trace 对齐。
		services.Configure<OpenTelemetryLoggerOptions>(logging =>
		{
			logging.IncludeScopes = true;
			logging.IncludeFormattedMessage = true;
		});

		if (configurePropagator)
		{
			// 传播器是进程级全局设置：默认不覆盖宿主配置，只有显式要求时才设置。
			UseEuoniaPropagator();
		}

		registration.Builder = builder;
		configure?.Invoke(builder);
		return services;
	}

	/// <summary>
	/// 把 Euonia 默认源目录加入跟踪构建器。
	/// </summary>
	/// <param name="builder">跟踪构建器。</param>
	/// <param name="registry">可选的运行时源注册表；仅包含构建 Provider 前已创建的源。</param>
	/// <param name="filter">可选的源名过滤器，用于排除静态组件源。</param>
	/// <returns>跟踪构建器。</returns>
	/// <remarks>
	/// OTel 的源订阅在 Provider 构建时固定：构建之后才创建的自定义源无法再导出。
	/// 需要导出的自定义源必须在 <see cref="AddEuoniaOpenTelemetry"/> 之前调用 <see cref="EuoniaSources.Add"/> 声明。
	/// </remarks>
	public static TracerProviderBuilder AddEuoniaSources(this TracerProviderBuilder builder, TelemetrySourceRegistry? registry = null, Func<string, bool>? filter = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		filter ??= static _ => true;

		foreach (var source in EuoniaSources.Default.Where(filter))
		{
			builder.AddSource(source);
		}

		if (registry != null)
		{
			foreach (var source in registry.GetAll().Select(item => item.SourceName).Where(filter))
			{
				builder.AddSource(source);
			}
		}

		return builder;
	}

	/// <summary>
	/// 把 Euonia 默认源目录加入指标构建器。
	/// </summary>
	/// <param name="builder">指标构建器。</param>
	/// <param name="registry">可选的运行时源注册表。</param>
	/// <param name="filter">可选的源名过滤器，用于排除静态组件源。</param>
	/// <returns>指标构建器。</returns>
	public static MeterProviderBuilder AddEuoniaSources(this MeterProviderBuilder builder, TelemetrySourceRegistry? registry = null, Func<string, bool>? filter = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		filter ??= static _ => true;

		foreach (var source in EuoniaSources.Default.Where(filter))
		{
			builder.AddMeter(source);
		}

		if (registry != null)
		{
			foreach (var source in registry.GetAll().Select(item => item.MeterName).Where(filter))
			{
				builder.AddMeter(source);
			}
		}

		return builder;
	}

	/// <summary>
	/// 设置 W3C 传播器（TraceContext + Baggage）。
	/// </summary>
	/// <remarks>进程级全局副作用：最后一次调用生效；宿主已自行配置传播器时不要调用。</remarks>
	public static void UseEuoniaPropagator()
	{
		Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
		[
			new TraceContextPropagator(),
			new BaggagePropagator(),
		]));
	}

	private sealed class EuoniaOpenTelemetryRegistration
	{
		public OpenTelemetryBuilder? Builder { get; set; }
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
