using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 把租户 schema / 隔离模式纳入 EF 模型缓存键，避免 SharedSchema 下不同租户复用同一模型。
/// </summary>
public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
	/// <inheritdoc />
	public object Create(DbContext context, bool designTime)
	{
		// 优先从上下文自身读取；AsyncLocal 仅作为设计时/手动构建模型的兼容回退。
		var tenant = (context as ITenantModelCacheKeySource)?.CurrentTenant;
		var (isolation, schema) = tenant != null
			                          ? (tenant.Isolation, tenant.Isolation == TenantIsolation.SharedSchema ? tenant.Schema : null)
			                          : TenantModelCacheKeyContext.Current;
		return new TenantModelCacheKey(context.GetType(), isolation, schema, designTime);
	}

	private sealed record TenantModelCacheKey(
		Type ContextType,
		TenantIsolation Isolation,
		string? Schema,
		bool DesignTime);
}
