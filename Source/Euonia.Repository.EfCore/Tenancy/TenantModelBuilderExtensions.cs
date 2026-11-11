using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 租户模型的 Fluent 配置扩展；允许不使用 <see cref="MultiTenantDataContextBase{TContext}"/> 的上下文接入租户过滤器。
/// </summary>
public static class TenantModelBuilderExtensions
{
	private static readonly MethodInfo _applyForMethod = typeof(TenantModelBuilderExtensions)
		.GetMethod(nameof(ApplyFor), BindingFlags.NonPublic | BindingFlags.Static)!;

	/// <summary>
	/// 为模型中的 <see cref="IMultiTenant"/> 实体配置影子属性、租户过滤器与租户索引。
	/// </summary>
	/// <typeparam name="TContext">数据上下文类型。</typeparam>
	/// <param name="modelBuilder">模型构建器。</param>
	/// <param name="context">实现 <see cref="ITenantFilterState"/> 的上下文；过滤器引用其属性以参与参数化。</param>
	/// <returns>模型构建器。</returns>
	public static ModelBuilder ApplyTenantModel<TContext>(this ModelBuilder modelBuilder, TContext context)
		where TContext : DbContext, ITenantFilterState
	{
		ArgumentNullException.ThrowIfNull(modelBuilder);
		ArgumentNullException.ThrowIfNull(context);

		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			// TPH 派生类型不能单独设置过滤器，统一由继承根承载。
			if (entityType.BaseType != null || !typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
			{
				continue;
			}

			_applyForMethod.MakeGenericMethod(entityType.ClrType, typeof(TContext))
			               .Invoke(null, [modelBuilder, context]);
		}

		return modelBuilder;
	}

	private static void ApplyFor<TEntity, TContext>(ModelBuilder modelBuilder, TContext context)
		where TEntity : class
		where TContext : DbContext, ITenantFilterState
	{
		var entity = modelBuilder.Entity<TEntity>();

		if (typeof(TEntity).GetProperty(nameof(IMultiTenantEntity.TenantId)) == null)
		{
			entity.Property<string>(nameof(IMultiTenantEntity.TenantId))
			      .HasMaxLength(64)
			      .IsRequired()
			      .HasColumnName("tenant_id");

			// 共享表下租户列是每次查询的第一过滤条件，默认建索引。
			entity.HasIndex(nameof(IMultiTenantEntity.TenantId));
		}

		Expression<Func<TEntity, bool>> filter = item =>
			context.IgnoreTenantFilter
			|| (context.CurrentTenantId != null
			    && EF.Property<string>(item, nameof(IMultiTenantEntity.TenantId)) == context.CurrentTenantId);

		modelBuilder.Model.FindEntityType(typeof(TEntity))!
		            .SetQueryFilter(TenantFilterNames.Tenant, filter);
	}
}
