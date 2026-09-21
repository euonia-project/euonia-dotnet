using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <inheritdoc />
public class EfCoreRepository<TContext, TEntity, TKey> : Repository<TContext, TEntity, TKey>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
	where TContext : DbContext, IRepositoryContext
{
	/// <summary>
	/// 初始化 <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于获取仓储上下文的上下文提供程序。</param>
	public EfCoreRepository(IContextProvider provider)
		: base(provider)
	{
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		//Context?.Dispose();
	}

	/// <inheritdoc />
	public override IQueryable<TEntity> Queryable()
	{
		var query = Context.Set<TEntity>().AsQueryable();
		if (Actions.Count > 0)
		{
			query = Actions.Aggregate(query, (current, action) => action(current));
		}

		return query;
	}

	/// <inheritdoc />
	public override IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		var query = Context.Set<TEntity>().AsQueryable();
		if (handle != null)
		{
			query = handle(query);
		}

		return query.Where(predicate);
	}

	/// <inheritdoc />
	public override async Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(key);
		return await Context.FindAsync<TEntity>([key], cancellationToken);
	}

	/// <inheritdoc />
	public override Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).FirstOrDefaultAsync(predicate, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).Skip(offset).Take(count).ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).CountAsync(predicate, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).LongCountAsync(predicate, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		// BuildQuery 已经应用了 Where(predicate)，此处不能再叠加一次：
		// Where(p).Any(p) 虽然等价，但会产生冗余的 SQL 条件。
		return BuildQuery(predicate, handle).AnyAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override async Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		// 此前写作 BuildQuery(predicate, handle).AllAsync(predicate)，即 Where(p).All(p)，
		// 翻译为 SQL 是 NOT EXISTS(... WHERE p AND NOT p)，**恒为 true**，断言完全失效。
		// All(p) ≡ !Any(!p)：应针对取反后的谓词查找反例。
		var negation = Expression.Lambda<Func<TEntity, bool>>(Expression.Not(predicate.Body), predicate.Parameters);
		var anyCounterExample = await BuildQuery(negation, handle).AnyAsync(cancellationToken).ConfigureAwait(false);
		return !anyCounterExample;
	}

	/// <inheritdoc />
	public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		var entry = await Context.AddAsync(entity, cancellationToken);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}

		return entry.Entity;
	}

	/// <inheritdoc />
	public override async Task InsertAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entities);
		await Context.AddRangeAsync(entities, cancellationToken);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}

	/// <inheritdoc />
	public override async Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		var _ = Context.Update(entity);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}

	/// <inheritdoc />
	public override async Task UpdateAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entities);
		Context.UpdateRange(entities);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}

	/// <inheritdoc />
	public override async Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		var _ = Context.Remove(entity);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}

	/// <inheritdoc />
	public override async Task DeleteAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entities);
		Context.RemoveRange(entities);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}
}