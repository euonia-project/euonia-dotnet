using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Nerosoft.Euonia.Repository.EfCore;

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
	/// Gets a value indicate whether the entry values are automatically set or not.
	/// </summary>
	protected abstract bool AutoSetEntryValues { get; }

	/// <summary>
	/// Gets the kind of the date time.
	/// </summary>
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
	/// 
	/// </summary>
	public Guid Id { get; }

	/// <summary>
	/// 
	/// </summary>
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
	/// Sets the entry values.
	/// </summary>
	/// <param name="entries"></param>
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