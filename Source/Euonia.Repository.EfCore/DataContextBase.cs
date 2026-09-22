using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

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
		SetEntryValues(entries);
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
	public virtual string Provider => Database.ProviderName;

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
	public IDbTransaction GetTransaction()
	{
		return Database.CurrentTransaction?.GetDbTransaction();
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
	/// 在保存变更前设置实体条目的值，派生类可重写以填充审计字段等。
	/// </summary>
	/// <param name="entries">变更跟踪器中当前被跟踪的所有实体条目。</param>
	protected virtual void SetEntryValues(IEnumerable<EntityEntry> entries)
	{
	}

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly, type => type.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(TContext));
		modelBuilder.SetTombstoneQueryFilter();
		base.OnModelCreating(modelBuilder);
	}
}