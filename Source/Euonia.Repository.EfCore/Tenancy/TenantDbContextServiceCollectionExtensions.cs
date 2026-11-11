using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 租户感知数据上下文的注册扩展。
/// </summary>
public static class TenantDbContextServiceCollectionExtensions
{
	/// <summary>
	/// 注册按租户路由连接串的数据上下文（DatabasePerTenant / SharedSchema）。
	/// </summary>
	/// <typeparam name="TContext">数据上下文类型。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <param name="configureProvider">使用连接串配置提供程序的委托（例如 <c>options.UseSqlServer(connection)</c>）。</param>
	/// <param name="lifetime">上下文生命周期；默认 Scoped。</param>
	/// <returns>注册完成后的服务集合。</returns>
	/// <remarks>
	/// options 生命周期固定为 Scoped：租户连接串与 schema 必须按作用域解析，不能缓存到根。
	/// </remarks>
	public static IServiceCollection AddTenantDbContext<TContext>(
		this IServiceCollection services,
		Action<DbContextOptionsBuilder, string> configureProvider,
		ServiceLifetime lifetime = ServiceLifetime.Scoped)
		where TContext : DbContext, IRepositoryContext
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configureProvider);

		services.AddDbContext<TContext>((provider, options) =>
		{
			var accessor = provider.GetRequiredService<ITenantAccessor>();
			var tenant = accessor.Current?.Tenant;

			var resolver = provider.GetRequiredService<ITenantConnectionStringResolver<TContext>>();
			var connectionString = resolver.Resolve(tenant)
			                       ?? throw new ConfigurationException(
				                       $"No connection string could be resolved for tenant '{tenant?.Id ?? "<none>"}'. Configure TenantInfo.ConnectionString or MultiTenancyOptions.ConnectionStringTemplate.");

			// 模型缓存键在 EF 内部提供程序中创建，无法解析应用服务；用 AsyncLocal 传递当前租户 schema。
			TenantModelCacheKeyContext.Set(
				tenant?.Isolation ?? TenantIsolation.SharedTable,
				tenant?.Isolation == TenantIsolation.SharedSchema ? tenant.Schema : null);

			configureProvider(options, connectionString);
			options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
		}, lifetime, ServiceLifetime.Scoped);

		return services;
	}
}
