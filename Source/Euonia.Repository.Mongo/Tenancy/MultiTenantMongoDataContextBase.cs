using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 租户感知的 MongoDB 数据上下文基类：提供租户过滤器与写入打标。
/// </summary>
/// <typeparam name="TContext">上下文自身类型。</typeparam>
/// <remarks>
/// MongoDB 没有全局查询过滤器，仓储查询必须显式调用 <see cref="TenantFilter{TEntity}"/> 或 <see cref="ApplyTenant{TEntity}"/>；
/// 本基类只负责提供一致的租户语义与写入校验，不修改仓储查询。
/// </remarks>
public abstract class MultiTenantMongoDataContextBase<TContext> : DataContextBase<TContext>, ITenantFilterState
	where TContext : MongoDbContext, IRepositoryContext
{
	private readonly ITenantAccessor _tenantAccessor;
	private readonly IOptions<MultiTenancyOptions>? _options;

	/// <summary>
	/// 初始化 <see cref="MultiTenantMongoDataContextBase{TContext}"/> 的新实例。
	/// </summary>
	/// <param name="database">MongoDB 数据库。</param>
	/// <param name="logger">日志工厂。</param>
	/// <param name="tenantAccessor">租户访问器。</param>
	/// <param name="options">多租户选项。</param>
	protected MultiTenantMongoDataContextBase(
		IMongoDatabase database,
		ILoggerFactory logger,
		ITenantAccessor tenantAccessor,
		IOptions<MultiTenancyOptions>? options = null)
		: base(database, logger)
	{
		_tenantAccessor = tenantAccessor;
		_options = options;
	}

	/// <inheritdoc />
	public string? CurrentTenantId => _tenantAccessor.TenantId;

	/// <inheritdoc />
	public bool IgnoreTenantFilter => _tenantAccessor.Current?.IsCrossTenant == true && CurrentTenantId == null;

	/// <summary>
	/// 获取当前租户状态。
	/// </summary>
	protected TenantStatus CurrentTenantStatus => _tenantAccessor.Current?.Tenant?.Status ?? TenantStatus.Active;

	/// <summary>
	/// 构造实体的租户过滤器。
	/// </summary>
	/// <typeparam name="TEntity">实体类型。</typeparam>
	/// <returns>租户过滤器。</returns>
	public FilterDefinition<TEntity> TenantFilter<TEntity>()
		where TEntity : IMultiTenant
	{
		return MongoTenantFilters.For<TEntity>(CurrentTenantId, IgnoreTenantFilter);
	}

	/// <summary>
	/// 把租户过滤器与业务过滤器组合。
	/// </summary>
	/// <typeparam name="TEntity">实体类型。</typeparam>
	/// <param name="filter">业务过滤器。</param>
	/// <returns>组合后的过滤器。</returns>
	public FilterDefinition<TEntity> ApplyTenant<TEntity>(FilterDefinition<TEntity> filter)
		where TEntity : IMultiTenant
	{
		return MongoTenantFilters.AndTenant(filter, CurrentTenantId, IgnoreTenantFilter);
	}

	/// <summary>
	/// 在写入前校验并打标租户。
	/// </summary>
	/// <typeparam name="TEntity">实体类型。</typeparam>
	/// <param name="entity">待写入实体。</param>
	/// <returns>写入租户标识。</returns>
	/// <exception cref="CrossTenantOperationException">实体租户与当前作用域不一致时抛出。</exception>
	/// <exception cref="TenantRequiredException">缺少租户时抛出。</exception>
	public string? StampTenant<TEntity>(TEntity entity)
		where TEntity : class, IMultiTenant
	{
		ArgumentNullException.ThrowIfNull(entity);

		if (CurrentTenantStatus != TenantStatus.Active && !(_options?.Value.AllowInactiveTenantWrites ?? false))
		{
			throw new TenantSuspendedException(CurrentTenantId ?? "-", CurrentTenantStatus);
		}

		var tenantId = entity is IMultiTenantEntity explicitEntity ? explicitEntity.TenantId : null;

		if (IgnoreTenantFilter)
		{
			throw new CrossTenantOperationException("The all-tenant cross-tenant scope is read-only; use a target tenant scope for writes.");
		}

		if (string.IsNullOrEmpty(CurrentTenantId))
		{
			throw new TenantRequiredException();
		}

		if (!string.IsNullOrEmpty(tenantId) && !string.Equals(tenantId, CurrentTenantId, StringComparison.Ordinal))
		{
			throw new CrossTenantOperationException($"Cannot write an entity of tenant '{tenantId}' inside tenant scope '{CurrentTenantId}'.");
		}

		if (entity is IMultiTenantEntity multiTenant)
		{
			multiTenant.TenantId = CurrentTenantId;
		}

		return CurrentTenantId;
	}
}
