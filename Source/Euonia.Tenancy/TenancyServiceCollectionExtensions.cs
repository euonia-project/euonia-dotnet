using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 多租户能力的服务注册扩展。
/// </summary>
public static class TenancyServiceCollectionExtensions
{
	/// <summary>
	/// 注册多租户核心服务。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">多租户选项配置委托。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddTenancy(this IServiceCollection services, Action<MultiTenancyOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		var options = services.AddOptions<MultiTenancyOptions>();

		// 宿主通常已注册 IConfiguration；控制台/测试等无配置场景不应因缺少绑定而失败。
		if (services.Any(x => x.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
		{
			options.BindConfiguration(MultiTenancyOptions.ConfigurationSection);
		}

		if (configure != null)
		{
			options.Configure(configure);
		}

		services.TryAddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
		services.TryAddSingleton<IExecutionContextScopeFactory, ExecutionContextScopeFactory>();

		services.TryAddSingleton<ITenantAccessor, TenantAccessor>();
		services.TryAddSingleton<ITenantScopeFactory, TenantScopeFactory>();
		services.TryAddScoped<ITenantResolver, TenantResolver>();
		services.TryAddScoped<ITenantAccessValidator, DefaultTenantAccessValidator>();
		services.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, TenantStartupValidator>());
		services.TryAddSingleton<IKeyTransformer, TenantKeyTransformer>();
		services.TryAddSingleton<ITenantMessageTrustPolicy, DefaultTenantMessageTrustPolicy>();
		services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolveContributor, MessageTenantResolveContributor>());

		return services;
	}

	/// <summary>
	/// 使用配置节中的租户列表注册租户存储。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddConfigurationTenantStore(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddSingleton<ConfigurationTenantStore>();
		services.TryAddSingleton(provider => new CachedTenantStore(
			provider.GetRequiredService<ConfigurationTenantStore>(),
			provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<MultiTenancyOptions>>()));
		services.TryAddSingleton<ITenantStore>(provider => provider.GetRequiredService<CachedTenantStore>());

		return services;
	}

	/// <summary>
	/// 使用内存租户集合注册租户存储，适用于测试与示例。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="tenants">初始租户集合。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddInMemoryTenantStore(this IServiceCollection services, IEnumerable<ITenantInfo>? tenants = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddSingleton(new InMemoryTenantStore(tenants));
		services.TryAddSingleton(provider => new CachedTenantStore(
			provider.GetRequiredService<InMemoryTenantStore>(),
			provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<MultiTenancyOptions>>()));
		services.TryAddSingleton<ITenantStore>(provider => provider.GetRequiredService<CachedTenantStore>());

		return services;
	}
}
