using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// ASP.NET Core 租户适配的服务注册扩展。
/// </summary>
public static class TenancyAspNetCoreServiceCollectionExtensions
{
	/// <summary>
	/// 注册租户核心服务与 HTTP 解析器。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">多租户选项配置委托。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddTenancyAspNetCore(this IServiceCollection services, Action<MultiTenancyOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddTenancy(configure);

		services.TryAddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
		services.TryAddSingleton<IExecutionContextScopeFactory, ExecutionContextScopeFactory>();

		services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolveContributor, ClaimTenantResolver>());
		services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolveContributor, HostTenantResolver>());
		services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolveContributor, RouteTenantResolver>());
		services.TryAddEnumerable(ServiceDescriptor.Scoped<ITenantResolveContributor, HeaderTenantResolver>());

		return services;
	}
}
