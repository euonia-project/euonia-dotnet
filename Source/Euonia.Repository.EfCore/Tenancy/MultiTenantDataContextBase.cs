using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Tenancy;

// 命名过滤器名称由 Tenancy 定义，避免 EF 适配层与核心契约各写一份字符串。

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 基于共享表的租户感知数据上下文基类：自动应用租户查询过滤器、写入打标与跨租户保护。
/// </summary>
/// <typeparam name="TContext">派生上下文自身的类型。</typeparam>
/// <remarks>
/// <para>
/// 租户列默认使用影子属性 <c>TenantId</c>（列名 <c>tenant_id</c>），实体无需声明属性；
/// 需要显式访问租户标识的实体实现 <see cref="IMultiTenantEntity"/> 即可复用 CLR 属性。
/// </para>
/// <para>
/// 过滤器按“软删除 + 租户”组合后一次性设置，兼容 EF Core 9/10/11 的替换语义；
/// TPH 派生类型跳过，避免“过滤器已在基类型定义”异常。
/// </para>
/// </remarks>
public abstract class MultiTenantDataContextBase<TContext> : DataContextBase<TContext>, ITenantFilterState
	where TContext : DbContext, IRepositoryContext
{
	private readonly ITenantAccessor _tenantAccessor;
	private readonly IOptions<MultiTenancyOptions>? _options;

	/// <summary>
	/// 初始化 <see cref="MultiTenantDataContextBase{TContext}"/> 的新实例。
	/// </summary>
	/// <param name="options">上下文选项。</param>
	/// <param name="tenantAccessor">租户访问器。</param>
	/// <param name="tenancyOptions">多租户选项；省略时按默认选项处理。</param>
	protected MultiTenantDataContextBase(
		DbContextOptions<TContext> options,
		ITenantAccessor tenantAccessor,
		IOptions<MultiTenancyOptions>? tenancyOptions = null)
		: base(options)
	{
		_tenantAccessor = tenantAccessor;
		_options = tenancyOptions;

		if (tenancyOptions is { Value.Enabled: false })
		{
			throw new ConfigurationException(
				$"'{GetType().FullName}' derives from MultiTenantDataContextBase but multi-tenancy is disabled. Enable 'Euonia:MultiTenancy:Enabled' or remove the tenant base class.");
		}
	}

	/// <summary>
	/// 获取当前数据作用域租户标识。
	/// </summary>
	public string? CurrentTenantId => _tenantAccessor.TenantId;

	/// <summary>
	/// 获取一个值，指示当前是否处于显式跨租户作用域。
	/// </summary>
	public bool IsCrossTenant => _tenantAccessor.Current?.IsCrossTenant ?? false;

	/// <summary>
	/// 获取跨租户目标租户标识。
	/// </summary>
	public string? TargetTenantId => _tenantAccessor.Current?.TargetTenantId;

	/// <summary>
	/// 获取一个值，指示是否跳过租户过滤器；仅全租户只读模式为 <see langword="true"/>。
	/// </summary>
	public bool IgnoreTenantFilter => IsCrossTenant && CurrentTenantId is null;

	/// <inheritdoc />
	protected override bool AutoSetEntryValues => true;

	/// <inheritdoc />
	protected override void ConfigureModel(ModelBuilder modelBuilder)
	{
		base.ConfigureModel(modelBuilder);

		var tenant = _tenantAccessor.Current?.Tenant;
		if (tenant is { Isolation: TenantIsolation.SharedSchema } && !string.IsNullOrWhiteSpace(tenant.Schema))
		{
			// SharedSchema：默认 schema 随租户变化；模型缓存键必须包含 schema（见 TenantModelCacheKeyFactory）。
			modelBuilder.HasDefaultSchema(tenant.Schema);
		}

		// EF Core 10/11 支持命名查询过滤器：租户过滤器与软删除过滤器共存，
		// 可用 IgnoreQueryFilters(["Tenant"]) 精确绕过，不再需要读取并重新组合既有过滤器。
		modelBuilder.ApplyTenantModel(this);
	}

	/// <inheritdoc />
	protected override void SetEntryValues(IEnumerable<EntityEntry> entries)
	{
		var status = _tenantAccessor.Current?.Tenant?.Status ?? TenantStatus.Active;
		var allowInactiveWrites = _options?.Value.AllowInactiveTenantWrites ?? false;

		foreach (var entry in entries)
		{
			if (entry.Entity is not IMultiTenant)
			{
				continue;
			}

			var operation = entry.State switch
			{
				EntityState.Added => TenantEntryOperation.Added,
				EntityState.Modified => TenantEntryOperation.Modified,
				EntityState.Deleted => TenantEntryOperation.Deleted,
				_ => TenantEntryOperation.None,
			};

			if (operation == TenantEntryOperation.None)
			{
				continue;
			}

			var (entityTenantId, originalTenantId) = GetTenantIds(entry, operation);
			var context = new TenantEntryContext(
				operation,
				entityTenantId,
				originalTenantId,
				CurrentTenantId,
				TargetTenantId,
				status,
				IsCrossTenant,
				allowInactiveWrites);

			var assigned = TenantEntryGuard.Validate(context);
			if (assigned != null)
			{
				SetTenantId(entry, assigned);
			}
		}

		base.SetEntryValues(entries);
	}

	private static (string? Current, string? Original) GetTenantIds(EntityEntry entry, TenantEntryOperation operation)
	{
		if (entry.Entity is IMultiTenantEntity entity)
		{
			var property = entry.Property(nameof(IMultiTenantEntity.TenantId));
			var current = operation == TenantEntryOperation.Added ? entity.TenantId : property.CurrentValue as string;
			var original = operation == TenantEntryOperation.Added ? null : property.OriginalValue as string;
			return (current, original);
		}

		var shadow = entry.Properties.FirstOrDefault(x => x.Metadata.Name == nameof(IMultiTenantEntity.TenantId));
		if (shadow == null)
		{
			return (null, null);
		}

		var shadowCurrent = operation == TenantEntryOperation.Added ? null : shadow.CurrentValue as string;
		var shadowOriginal = operation == TenantEntryOperation.Added ? null : shadow.OriginalValue as string;
		return (shadowCurrent, shadowOriginal);
	}

	private static void SetTenantId(EntityEntry entry, string tenantId)
	{
		if (entry.Entity is IMultiTenantEntity entity)
		{
			entity.TenantId = tenantId;
			return;
		}

		entry.Property(nameof(IMultiTenantEntity.TenantId)).CurrentValue = tenantId;
	}


}
