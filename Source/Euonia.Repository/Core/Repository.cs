using System.Linq.Expressions;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// The abstract implement of <see cref="IRepository{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TKey">The type of the entity key.</typeparam>
/// <typeparam name="TContext"></typeparam>
public abstract class Repository<TContext, TEntity, TKey> : DisposableObject, IRepository<TContext, TEntity, TKey>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
	where TContext : class, IRepositoryContext
{
	private readonly IContextProvider _provider;

	/// <summary>
	/// Initialize a new instance of <see cref="Repository{TContext, TEntity, TKey}"/> with context.
	/// </summary>
	/// <param name="provider"></param>
	protected Repository(IContextProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// Gets the data persistent context.
	/// </summary>
	public TContext Context => _provider.GetContext<TContext>();

	/// <inheritdoc />
	public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> Actions { get; } = new();

	/// <summary>
	/// Gets all entities.
	/// </summary>
	/// <returns></returns>
	public abstract IQueryable<TEntity> Queryable();

	/// <inheritdoc />
	public abstract IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle);

	/// <inherited/>
	public abstract Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task<TEntity> InsertAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task InsertAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task UpdateAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inherited/>
	public abstract Task DeleteAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Commit changes asynchronously.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return await Context.SaveChangesAsync(cancellationToken);
	}
}