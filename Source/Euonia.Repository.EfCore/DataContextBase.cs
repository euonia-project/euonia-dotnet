using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Nerosoft.Euonia.Repository;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 基于 EF Core 的仓储上下文基类，在 <see cref="DbContext"/> 之上实现 <see cref="IRepositoryContext"/>。
/// </summary>
/// <typeparam name="TContext">派生上下文自身的类型。</typeparam>
/// <inheritdoc cref="DbContext" />
public abstract class DataContextBase<TContext> : DbContext, IRepositoryContext
	where TContext : DbContext, IRepositoryContext
{
	/// <inheritdoc />
	protected DataContextBase(DbContextOptions<TContext> options)
		: base(options)
	{
		Id = Guid.NewGuid();
	}

	/// <summary>
	/// 获取一个值，指示是否自动设置实体条目的审计相关值。
	/// </summary>
	/// <value>若需要在保存时自动填充条目值则为 <c>true</c>，否则为 <c>false</c>。</value>
	protected abstract bool AutoSetEntryValues { get; }

	/// <summary>
	/// 获取写入日期时间值时使用的 <see cref="System.DateTimeKind"/>。
	/// </summary>
	/// <value>默认值为 <see cref="DateTimeKind.Unspecified"/>。</value>
	protected virtual DateTimeKind DateTimeKind { get; } = DateTimeKind.Unspecified;

	/// <inheritdoc />
	public override int SaveChanges()
	{
		return SaveChanges(true);
	}

	/// <inheritdoc />
	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		var entries = ChangeTracker.Entries();
		if (AutoSetEntryValues)
		{
			SetEntryValues(entries);
		}

		var result = base.SaveChanges(acceptAllChangesOnSuccess);
		return result;
	}

	#region Implementation of IRepositoryContext

	/// <summary>
	/// 获取当前上下文的唯一标识，在构造上下文时生成。
	/// </summary>
	/// <value>上下文实例的标识。</value>
	public Guid Id { get; }

	/// <summary>
	/// 获取当前上下文所使用的数据库提供程序名称。
	/// </summary>
	/// <value>EF Core 数据提供程序的名称。</value>
	public virtual string Provider => Database.ProviderName!;

	/// <inheritdoc />
	public IQueryable<TEntity> SetOf<TEntity>()
		where TEntity : class
	{
		return Set<TEntity>();
	}

	/// <inheritdoc />
	public IDbConnection GetConnection()
	{
		return Database.GetDbConnection();
	}

	/// <inheritdoc />
	public IDbTransaction? GetTransaction()
	{
		return Database.CurrentTransaction?.GetDbTransaction();
	}

	/// <inheritdoc />
	public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		return Database.BeginTransactionAsync(cancellationToken);
	}

	/// <inheritdoc />
	public Task CommitAsync(CancellationToken cancellationToken = default)
	{
		return Database.CommitTransactionAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		await Database.RollbackTransactionAsync(cancellationToken);
	}

	/// <inheritdoc />
	public virtual IEnumerable<object> GetTrackedEntries()
	{
		var entries = ChangeTracker.Entries();

		return entries;
	}

	#endregion

	/// <inheritdoc cref="DbContext.SaveChangesAsync(CancellationToken)" />
	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return await SaveChangesAsync(true, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		var entries = ChangeTracker.Entries();
		if (AutoSetEntryValues)
		{
			SetEntryValues(entries);
		}

		var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		return result;
	}

	/// <summary>
	/// 获取创建人字段的名称；默认值为 <c>CreatedBy</c>。
	/// </summary>
	protected virtual string CreatedByFieldName => "CreatedBy";

	/// <summary>
	/// 获取更新人字段的名称；默认值为 <c>UpdatedBy</c>。
	/// </summary>
	protected virtual string UpdatedByFieldName => "UpdatedBy";

	/// <summary>
	/// 获取删除人字段的名称；默认值为 <c>DeletedBy</c>。
	/// </summary>
	protected virtual string DeletedByFieldName => "DeletedBy";

	/// <summary>
	/// 获取当前用户的标识；返回 <c>null</c> 时仅跳过操作用户字段（CreatedBy/UpdatedBy/DeletedBy）的写入，
	/// 时间戳填充与软删除转换仍会执行（匿名/后台场景）。
	/// </summary>
	/// <returns>当前用户的标识，类型由实体上的操作用户字段决定。</returns>
	protected virtual object? GetCurrentUserId()
	{
		return null;
	}

	/// <summary>
	/// 在保存变更前设置实体条目的值：为实现了 <see cref="IHasCreateTime"/>、<see cref="IHasUpdateTime"/>、
	/// <see cref="IHasDeleteTime"/> 或 <see cref="ITombstone"/> 的实体填充审计字段，并将删除转换为软删除。
	/// 当前用户标识为 <c>null</c>（匿名/后台场景）时，时间戳填充与软删除转换仍会执行，仅跳过操作用户字段的写入。
	/// </summary>
	/// <param name="entries">变更跟踪器中当前被跟踪的所有实体条目。</param>
	protected virtual void SetEntryValues(IEnumerable<EntityEntry> entries)
	{
		var userId = GetCurrentUserId();

		var now = DateTimeKind switch
		{
			DateTimeKind.Utc => DateTime.UtcNow,
			DateTimeKind.Local => DateTime.Now,
			_ => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
		};

		foreach (var entry in entries)
		{
			switch (entry.State)
			{
				case EntityState.Added:
					if (entry.Entity is IHasCreateTime created)
					{
						created.CreatedAt = now;
						if (userId is not null)
						{
							entry.TrySetProperty(CreatedByFieldName, userId);
						}
					}

					if (entry.Entity is IHasUpdateTime added)
					{
						added.UpdatedAt = now;
						if (userId is not null)
						{
							entry.TrySetProperty(UpdatedByFieldName, userId);
						}
					}

					break;

				case EntityState.Modified:
					if (entry.Entity is IHasUpdateTime modified)
					{
						modified.UpdatedAt = now;
						if (userId is not null)
						{
							entry.TrySetProperty(UpdatedByFieldName, userId);
						}
					}

					// IsDeleted 可能未映射（如 [NotMapped]），entry.Property 对未映射属性会抛异常，
					// 先检查元数据判空，与 TrySetProperty 的防御风格一致。
					if (entry.Entity is ITombstone { IsDeleted: true }
					    && entry.Metadata.FindProperty(nameof(ITombstone.IsDeleted)) is not null
					    && entry.Property(nameof(ITombstone.IsDeleted)).OriginalValue is false)
					{
						if (entry.Entity is IHasDeleteTime deleted)
						{
							deleted.DeletedAt = now;
						}

						if (userId is not null)
						{
							entry.TrySetProperty(DeletedByFieldName, userId);
						}
					}

					break;

				case EntityState.Deleted when entry.Entity is ITombstone tombstone:
					tombstone.IsDeleted = true;
					if (entry.Entity is IHasDeleteTime deleteTime)
					{
						deleteTime.DeletedAt = now;
					}

					if (userId is not null)
					{
						entry.TrySetProperty(DeletedByFieldName, userId);
					}

					if (entry.Entity is IHasUpdateTime deletedWithUpdateTime)
					{
						deletedWithUpdateTime.UpdatedAt = now;
						if (userId is not null)
						{
							entry.TrySetProperty(UpdatedByFieldName, userId);
						}
					}

					entry.State = EntityState.Modified;

					// 转为 Modified 会把所有属性标记为已修改；stub 删除（实体仅含主键）时其余属性的
					// CurrentValue 与 OriginalValue 均为 default，若全部写入会用默认值覆盖数据库中的真实数据。
					// 因此对 CurrentValue 等于 OriginalValue 的非审计属性取消修改标记，
					// 使生成的 UPDATE 仅包含实际变化的列（审计列 + 主键）。
					foreach (var property in entry.Properties)
					{
						if (!property.IsModified) continue;
						var name = property.Metadata.Name;
						// 审计列始终保持修改标记
						if (name == nameof(ITombstone.IsDeleted)
						    || name == nameof(IHasDeleteTime.DeletedAt)
						    || name == DeletedByFieldName
						    || name == nameof(IHasUpdateTime.UpdatedAt)
						    || name == UpdatedByFieldName)
						{
							continue;
						}

						// 主键列不参与 UPDATE，保持 EF 默认行为
						if (property.Metadata.IsPrimaryKey()) continue;

						// 仅当 CurrentValue 与 OriginalValue 相等时才取消修改标记；
						// 已加载实体上用户主动修改过的非审计列仍会被写入。
						if (Equals(property.CurrentValue, property.OriginalValue))
						{
							property.IsModified = false;
						}
					}

					break;
			}
		}
	}

	/// <summary>
	/// 将指定属性标记为已修改；属性未在模型中映射时不做任何操作。
	/// </summary>
	/// <param name="entry">实体项。</param>
	/// <param name="propertyName">属性名称。</param>
	private static void MarkPropertyModified(EntityEntry entry, string propertyName)
	{
		if (entry.Metadata.FindProperty(propertyName) is not null)
		{
			entry.Property(propertyName).IsModified = true;
		}
	}

	/// <summary>
	/// 在应用实体配置与软删除过滤器之后、模型最终化之前配置模型，供派生上下文追加租户等全局过滤器。
	/// </summary>
	/// <param name="modelBuilder">要配置的模型构建器。</param>
	protected virtual void ConfigureModel(ModelBuilder modelBuilder)
	{
	}

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly, type => type.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(TContext));
		modelBuilder.SetTombstoneQueryFilter();
		ConfigureModel(modelBuilder);
		base.OnModelCreating(modelBuilder);
	}
}