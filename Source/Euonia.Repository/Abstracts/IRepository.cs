using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> : IDisposable
	where TEntity : class
{
	/// <summary>
	/// Gets the actions to be executed before query.
	/// </summary>
	List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> Actions { get; }

	/// <summary>
	/// Queryable this instance.
	/// </summary>
	/// <returns>The all.</returns>
	IQueryable<TEntity> Queryable();

	/// <summary>
	/// Builds query.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <returns></returns>
	IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle);

	/// <summary>
	/// Gets single element that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return GetAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// Gets single element that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds elements in a sequence that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return FindAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// Finds elements in a sequence that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds elements in a sequence that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="offset"></param>
	/// <param name="count"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, int offset, int count, CancellationToken cancellationToken = default)
	{
		return FindAsync(predicate, null, offset, count, cancellationToken);
	}

	/// <summary>
	/// Finds elements in a sequence that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <param name="offset"></param>
	/// <param name="count"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets number of elements in a sequence that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return CountAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// Gets number of elements in a sequence that satisfy a condition asynchronously.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets number of elements in a sequence that satisfy a condition asynchronously and returns an <see cref="long"/> value.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return LongCountAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// Gets number of elements in a sequence that satisfy a condition asynchronously and returns an <see cref="long"/> value.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Determines whether any element of a sequence satisfies a condition asynchronously.
	/// </summary>
	/// <param name="predicate">The condition.</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return AnyAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// Asynchronously determines whether any element of a sequence satisfies a condition.
	/// </summary>
	/// <param name="predicate">The condition.</param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Asynchronously determines whether all the elements of a sequence satisfy a condition.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return AllAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// Asynchronously determines whether all the elements of a sequence satisfy a condition.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Insert a new entity asynchronously.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="autoSave"></param>
	/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
	/// <returns></returns>
	Task<TEntity> InsertAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Insert multiple new entities asynchronously.
	/// </summary>
	/// <param name="entities"></param>
	/// <param name="autoSave"></param>
	/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
	/// <returns></returns>
	Task InsertAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Update an exists entity asynchronously.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <param name="autoSave">A <see cref="bool"/> value indicate that whether the <see cref="SaveChangesAsync" /> will called automatically.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
	/// <returns></returns>
	Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Update multiple exists entities asynchronously.
	/// </summary>
	/// <param name="entities"></param>
	/// <param name="autoSave">A <see cref="bool"/> value indicate that whether the <see cref="SaveChangesAsync" /> will called automatically.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
	/// <returns></returns>
	Task UpdateAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// DeleteAsync the specified entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="autoSave"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// DeleteAsync the specified entities.
	/// </summary>
	/// <param name="entities"></param>
	/// <param name="autoSave"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task DeleteAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves the changes async.
	/// </summary>
	/// <returns>The changes async.</returns>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IRepository<TEntity, in TKey> : IRepository<TEntity>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
{
	/// <summary>
	/// Gets an entity with the given primary key value asynchronously.
	/// </summary>
	/// <param name="key">The key of entity.</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default)
	{
		return GetAsync(key, null, cancellationToken);
	}

	/// <summary>
	/// Gets an entity with the given primary key value asynchronously.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<TEntity> GetAsync(TKey key, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(key);
		return GetAsync(PredicateBuilder.PropertyEqual<TEntity, TKey>(nameof(IEntity<TKey>.Id), key), handle, cancellationToken);
	}

	/// <summary>
	/// Finds elemets in a sequence with the given primary key values asynchronously.
	/// </summary>
	/// <param name="keys"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<List<TEntity>> FindAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
	{
		return FindAsync(keys, null, cancellationToken);
	}

	/// <summary>
	/// Finds elements in a sequence with the given primary key values asynchronously.
	/// </summary>
	/// <param name="keys"></param>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<List<TEntity>> FindAsync(IEnumerable<TKey> keys, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(keys);
		if (!keys.Any())
		{
			return Task.FromResult(new List<TEntity>());
		}

		return FindAsync(PredicateBuilder.PropertyInRange<TEntity, TKey>(nameof(IEntity<>.Id), keys.ToArray()), handle, cancellationToken);
	}
}

/// <summary>
/// The interface of repository.
/// </summary>
/// <typeparam name="TEntity">The type if entity.</typeparam>
/// <typeparam name="TKey">The type of entity identifier.</typeparam>
/// <typeparam name="TContext">The type of context.</typeparam>
public interface IRepository<out TContext, TEntity, in TKey> : IRepository<TEntity, TKey>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
	where TContext : class, IRepositoryContext
{
	/// <summary>
	/// Gets the context.
	/// </summary>
	/// <value>The context.</value>
	TContext Context { get; }
}