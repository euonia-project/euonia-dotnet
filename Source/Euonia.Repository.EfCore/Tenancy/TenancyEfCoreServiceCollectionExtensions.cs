using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Tenancy;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// EF Core 租户适配的服务注册扩展。
/// </summary>
public static class TenancyEfCoreServiceCollectionExtensions
{
	/// <summary>
	/// 为租户感知数据上下文注册 EF Core 适配服务。
	/// </summary>
	/// <typeparam name="TContext">数据上下文类型。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <returns>注册完成后的服务集合。</returns>
	/// <exception cref="ConfigurationException">使用 <c>AddDbContextPool</c> 时抛出。</exception>
	/// <remarks>
	/// 租户上下文必须使用 Scoped 的 <c>DbContextOptions</c>；<c>AddDbContextPool</c> 不会重新执行构造函数，
	/// 租户状态会串流。此处只做 DI 层探测，动态构造的上下文仍需遵守文档约束。
	/// </remarks>
	public static IServiceCollection AddTenantEfCore<TContext>(this IServiceCollection services)
		where TContext : DbContext, IRepositoryContext
	{
		ArgumentNullException.ThrowIfNull(services);

		// IDbContextPool<T> 位于 EF Core 内部命名空间；按服务类型形状探测，避免依赖 internal 命名空间。
		if (services.Any(x => x.ServiceType.IsGenericType
		                      && x.ServiceType.GetGenericTypeDefinition().Name == "IDbContextPool`1"))
		{
			throw new ConfigurationException(
				$"'{typeof(TContext).FullName}' is registered with AddDbContextPool, which is not compatible with tenant-aware connection routing. Use AddDbContext or a scoped AddDbContextFactory instead.");
		}

		services.TryAddScoped<ITenantConnectionStringResolver<TContext>, TenantConnectionStringResolver<TContext>>();
		return services;
	}
}
