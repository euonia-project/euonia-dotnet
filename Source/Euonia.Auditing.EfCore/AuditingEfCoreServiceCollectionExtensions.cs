using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.EfCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 审计 EF Core 存储的注册扩展。
/// </summary>
public static class AuditingEfCoreServiceCollectionExtensions
{
	/// <summary>
	/// 注册独立审计库与 EF Core 存储。
	/// </summary>
	/// <typeparam name="TContext">审计上下文类型。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">数据上下文选项配置委托。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddAuditingEfCore<TContext>(
		this IServiceCollection services,
		Action<DbContextOptionsBuilder> configure)
		where TContext : AuditingDbContext
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configure);

		// 审计存储是单例；上下文工厂为单例，连接串固定为审计库。
		services.AddDbContextFactory<TContext>(configure, ServiceLifetime.Singleton);
		if (!services.Any(descriptor => descriptor.ServiceType == typeof(IAuditingStore)))
		{
			services.AddSingleton<IAuditingStore, EfCoreAuditingStore<TContext>>();
			services.AddSingleton<IAuditingStoreQuery>(provider => (IAuditingStoreQuery)provider.GetRequiredService<IAuditingStore>());
		}

		return services;
	}
}
