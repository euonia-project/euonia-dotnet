using System.Linq.Expressions;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// <see cref="IRepository{TEntity, TKey}"/> 的抽象实现。
/// </summary>
/// <typeparam name="TEntity">实体的类型。</typeparam>
/// <typeparam name="TKey">实体键的类型。</typeparam>
/// <typeparam name="TContext">数据持久化上下文的类型。</typeparam>
public abstract class Repository<TContext, TEntity, TKey> : DisposableObject, IRepository<TContext, TEntity, TKey>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
	where TContext : class, IRepositoryContext
{
	private readonly IContextProvider _provider;

	/// <summary>
	/// 使用上下文初始化 <see cref="Repository{TContext, TEntity, TKey}"/> 的新实例。
	/// </summary>
	/// <param name="provider">用于获取数据持久化上下文的上下文提供程序。</param>
	protected Repository(IContextProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 获取数据持久化上下文。
	/// </summary>
	public TContext Context => _provider.GetContext<TContext>();

	/// <inheritdoc />
	public List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> Actions { get; } = new();

	/// <summary>
	/// 获取所有实体。
	/// </summary>
	/// <returns>可查询所有实体的 <see cref="IQueryable{T}"/>。</returns>
	public abstract IQueryable<TEntity> Queryable();

	/// <inheritdoc />
	public abstract IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle);

	/// <inheritdoc/>
	public abstract Task<TEntity?> GetAsync(TKey key, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, int offset, int count, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>>? handle, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task<TEntity> InsertAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task InsertAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task UpdateAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <inheritdoc/>
	public abstract Task DeleteAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步提交更改。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>已提交的更改数量。</returns>
	public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return await Context.SaveChangesAsync(cancellationToken);
	}
}