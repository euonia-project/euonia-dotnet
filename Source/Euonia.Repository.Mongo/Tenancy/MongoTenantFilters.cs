using MongoDB.Driver;
using Nerosoft.Euonia.MultiTenancy;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// MongoDB 租户过滤器构造。
/// </summary>
public static class MongoTenantFilters
{
	/// <summary>
	/// 构造租户过滤器；全租户跨租户模式下返回空过滤器。
	/// </summary>
	/// <typeparam name="TEntity">实体类型。</typeparam>
	/// <param name="tenantId">数据作用域租户标识。</param>
	/// <param name="ignore">是否跳过租户过滤（仅全租户只读模式）。</param>
	/// <returns>租户过滤器。</returns>
	/// <remarks>
	/// 使用 BSON 字段名 <c>TenantId</c>；共享集合建议为 <c>(TenantId, ...)</c> 建复合索引。
	/// </remarks>
	public static FilterDefinition<TEntity> For<TEntity>(string? tenantId, bool ignore)
		where TEntity : IMultiTenant
	{
		if (ignore)
		{
			return FilterDefinition<TEntity>.Empty;
		}

		// 无租户时使用空集合过滤：匹配不到任何文档（fail-closed），也不引入可能与业务值冲突的哨兵。
		return string.IsNullOrEmpty(tenantId)
			       ? Builders<TEntity>.Filter.In<string>("TenantId", Array.Empty<string>())
			       : Builders<TEntity>.Filter.Eq("TenantId", tenantId);
	}

	/// <summary>
	/// 把租户过滤器与业务过滤器组合。
	/// </summary>
	/// <typeparam name="TEntity">实体类型。</typeparam>
	/// <param name="filter">业务过滤器。</param>
	/// <param name="tenantId">数据作用域租户标识。</param>
	/// <param name="ignore">是否跳过租户过滤。</param>
	/// <returns>组合后的过滤器。</returns>
	public static FilterDefinition<TEntity> AndTenant<TEntity>(FilterDefinition<TEntity> filter, string? tenantId, bool ignore)
		where TEntity : IMultiTenant
	{
		ArgumentNullException.ThrowIfNull(filter);
		return Builders<TEntity>.Filter.And(filter, For<TEntity>(tenantId, ignore));
	}
}
