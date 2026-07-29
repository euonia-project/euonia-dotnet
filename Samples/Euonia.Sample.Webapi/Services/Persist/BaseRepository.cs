using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// Provides a base repository implementation for Entity Framework Core that extends
/// <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> and exposes common helper methods.
/// </summary>
/// <typeparam name="TContext">The DbContext type deriving from <see cref="DataContextBase{TContext}"/>.</typeparam>
/// <typeparam name="TEntity">The entity type managed by the repository. Must implement <see cref="Euonia.Domain.IEntity{TKey}"/>.</typeparam>
/// <typeparam name="TKey">The type of the entity primary key. Must implement <see cref="IEquatable{TKey}"/>.</typeparam>
internal abstract class BaseRepository<TContext, TEntity, TKey> : EfCoreRepository<TContext, TEntity, TKey>, IScopedDependency
	where TContext : DataContextBase<TContext>
	where TEntity : class, Repository.IEntity<TKey>
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseRepository{TContext, TEntity, TKey}"/> class.
	/// </summary>
	/// <param name="provider">The context provider used to obtain the EF Core <see cref="DbContext"/> instance.</param>
	protected BaseRepository(IContextProvider provider)
		: base(provider)
	{
	}

	/// <summary>
	/// Gets a queryable set for the specified entity type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The entity CLR type to query. Must be a class.</typeparam>
	/// <returns>An <see cref="IQueryable{T}"/> for the requested entity type that can be composed and executed against the current <see cref="DbContext"/>.</returns>
	public IQueryable<T> SetOf<T>()
		where T : class
	{
		return Context.SetOf<T>();
	}

	/// <summary>
	/// Gets the first entity that has the specified primary key.
	/// </summary>
	/// <param name="id">The primary key value to search for.</param>
	/// <param name="tracking">If <c>true</c>, the returned entity is tracked by the context; otherwise tracking is disabled.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the entity instance if found; otherwise <c>null</c>.
	/// </returns>
	public virtual Task<TEntity> GetAsync(TKey id, bool tracking, CancellationToken cancellationToken = default)
	{
		//var lambda = predicate.Compile();
		return GetAsync(id, tracking, [], cancellationToken);
	}

	/// <summary>
	/// Gets the first entity that has the specified primary key and optionally includes navigation properties.
	/// </summary>
	/// <param name="id">The primary key value to search for.</param>
	/// <param name="tracking">If <c>true</c>, the returned entity is tracked by the context; otherwise tracking is disabled.</param>
	/// <param name="properties">An array of navigation property names to include in the query. Can be empty.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the entity instance if found; otherwise <c>null</c>.
	/// </returns>
	public virtual Task<TEntity> GetAsync(TKey id, bool tracking, string[] properties, CancellationToken cancellationToken = default)
	{
		var predicate = PredicateBuilder.PropertyEqual<TEntity, TKey>(nameof(Euonia.Domain.IEntity<TKey>.Id), id);

		//var lambda = predicate.Compile();
		return GetAsync(predicate, tracking, properties, cancellationToken);
	}

	/// <summary>
	/// Gets the first entity that matches the specified predicate.
	/// </summary>
	/// <param name="predicate">The predicate expression used to filter entities.</param>
	/// <param name="tracking">If <c>true</c>, the returned entity is tracked by the context; otherwise tracking is disabled.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the entity instance if found; otherwise <c>null</c>.
	/// </returns>
	public virtual Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracking, CancellationToken cancellationToken = default)
	{
		return GetAsync(predicate, tracking, [], cancellationToken);
	}

	/// <summary>
	/// Gets the first entity that matches the specified predicate and optionally includes navigation properties.
	/// </summary>
	/// <param name="predicate">The predicate expression used to filter entities.</param>
	/// <param name="tracking">If <c>true</c>, the returned entity is tracked by the context; otherwise tracking is disabled.</param>
	/// <param name="properties">An array of navigation property names to include in the query. Can be empty.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the entity instance if found; otherwise <c>null</c>.
	/// </returns>
	public virtual Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracking, string[] properties, CancellationToken cancellationToken = default)
	{
		return base.GetAsync(predicate, query => BuildQuery(query, tracking, properties), cancellationToken);
	}

	public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
	{
		return Context.Set<TEntity>().AnyAsync(expression, cancellationToken);
	}

	/// <summary>
	/// Finds entities whose primary keys are contained in the provided collection.
	/// </summary>
	/// <param name="ids">A collection of primary key values to search for.</param>
	/// <param name="properties">An array of navigation property names to include in the query. Can be empty.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains a list of matching entities (may be empty).
	/// </returns>
	public virtual Task<List<TEntity>> FindAsync(IEnumerable<TKey> ids, string[] properties, CancellationToken cancellationToken = default)
	{
		var predicate = PredicateBuilder.PropertyInRange<TEntity, TKey>(nameof(Euonia.Domain.IEntity<TKey>.Id), ids.ToArray());
		return FindAsync(predicate, properties, cancellationToken);
	}

	/// <summary>
	/// Finds entities that match the specified predicate and optionally includes navigation properties.
	/// </summary>
	/// <param name="predicate">The predicate expression used to filter entities.</param>
	/// <param name="properties">An array of navigation property names to include in the query. Can be empty.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains a list of matching entities (may be empty).
	/// </returns>
	public virtual Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, string[] properties, CancellationToken cancellationToken = default)
	{
		return base.FindAsync(predicate, query => BuildQuery(query, false, properties), cancellationToken);
	}

	/// <summary>
	/// Finds entities based on the given handle.
	/// </summary>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public virtual Task<List<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var query = Context.Set<TEntity>().AsNoTracking();
		if (handle != null)
		{
			_ = handle(query);
		}

		return query.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Counts entities based on the given handle.
	/// </summary>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public virtual Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var query = Context.Set<TEntity>().AsNoTracking();
		if (handle != null)
		{
			_ = handle(query);
		}

		return query.CountAsync(cancellationToken);
	}

	/// <summary>
	/// Gets a single entity based on the given handle.
	/// </summary>
	/// <param name="handle"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public virtual Task<TEntity> GetAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var query = Context.Set<TEntity>().AsNoTracking();
		if (handle != null)
		{
			_ = handle(query);
		}

		return query.FirstOrDefaultAsync(cancellationToken);
	}

	/// <summary>
	/// Looks up entities by their IDs and selects a key-value pair using the provided selector.
	/// </summary>
	/// <param name="ids">The values of the primary key for the entity to be found.</param>
	/// <param name="selector"></param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
	/// <returns></returns>
	public virtual async Task<Dictionary<TKey, string>> LookupAsync(IEnumerable<TKey> ids, Expression<Func<TEntity, KeyValuePair<TKey, string>>> selector, CancellationToken cancellationToken = default)
	{
		var predicate = PredicateBuilder.PropertyInRange<TEntity, TKey>(nameof(Euonia.Domain.IEntity<TKey>.Id), ids.ToArray());
		var query = Context.Set<TEntity>().AsNoTracking()
						   .Where(predicate)
						   .Select(selector);

		var items = await query.ToListAsync(cancellationToken: cancellationToken);

		var result = items.ToDictionary(x => x.Key, x => x.Value);

		return result;
	}

	/// <summary>
	/// Deletes the entity with the specified primary key.
	/// </summary>
	/// <param name="id">The primary key of the entity to delete.</param>
	/// <param name="autoSave">If <c>true</c>, calls <see cref="SaveChangesAsync(CancellationToken)"/> after removal.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	/// <exception cref="NotFoundException">Thrown when an entity with the specified id cannot be found.</exception>
	public virtual async Task DeleteAsync(TKey id, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		var set = Context.Set<TEntity>();

		var entity = await set.FindAsync([id], cancellationToken: cancellationToken);
		if (entity is null)
		{
			throw new NotFoundException();
		}

		set.Remove(entity);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}

	/// <summary>
	/// Deletes the entity with the specified primary key and raises a domain event produced by <paramref name="eventFactory"/>.
	/// </summary>
	/// <typeparam name="TEvent">The domain event type to raise.</typeparam>
	/// <param name="id">The primary key of the entity to delete.</param>
	/// <param name="eventFactory">A factory that creates a domain event from the entity before deletion.</param>
	/// <param name="autoSave">If <c>true</c>, calls <see cref="SaveChangesAsync(CancellationToken)"/> after removal.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	/// <exception cref="NotFoundException">Thrown when an entity with the specified id cannot be found.</exception>
	public virtual async Task DeleteAsync<TEvent>(TKey id, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent
	{
		var set = Context.Set<TEntity>();

		var entity = await set.FindAsync([id], cancellationToken: cancellationToken);
		switch (entity)
		{
			case null:
				throw new NotFoundException();
			case IHasDomainEvents aggregate:
				{
					var @event = eventFactory();
					aggregate.RaiseEvent(@event);
					break;
				}
		}

		set.Remove(entity);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}


	/// <summary>
	/// Deletes the specified entity and optionally raises a domain event produced by <paramref name="eventFactory"/>.
	/// </summary>
	/// <typeparam name="TEvent">The domain event type to raise.</typeparam>
	/// <param name="entity">The entity instance to delete.</param>
	/// <param name="eventFactory">A factory that creates the domain event; invoked only when the entity implements <see cref="IHasDomainEvents"/>.</param>
	/// <param name="autoSave">If <c>true</c>, calls <see cref="SaveChangesAsync(CancellationToken)"/> after removal.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	public virtual Task DeleteAsync<TEvent>(TEntity entity, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent
	{
		if (entity is IHasDomainEvents aggregate)
		{
			var @event = eventFactory();
			aggregate.RaiseEvent(@event);
		}

		{
		}

		return DeleteAsync(entity, autoSave, cancellationToken);
	}

	/// <summary>
	/// Persists changes in the current <see cref="DbContext"/> to the underlying database.
	/// This method overrides the base implementation and forwards the call to <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
	/// <returns>
	/// A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.
	/// </returns>
	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return Context.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Builds a base query for the repository by applying tracking behavior and include paths.
	/// </summary>
	/// <param name="query">The base queryable to modify.</param>
	/// <param name="tracking">If <c>true</c>, the returned query will use change tracking via <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsTracking{TEntity}(IQueryable{TEntity})"/>; otherwise <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/> is applied.</param>
	/// <param name="properties">Array of navigation property names to include. Each entry is passed to <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include{TEntity, TProperty}(IQueryable{TEntity}, string)"/>.</param>
	/// <returns>The composed <see cref="IQueryable{TEntity}"/> with tracking and includes applied.</returns>
	protected virtual IQueryable<TEntity> BuildQuery(IQueryable<TEntity> query, bool tracking, string[] properties)
	{
		query = tracking ? query.AsTracking() : query.AsNoTracking();

		if (properties is { Length: > 0 })
		{
			query = properties.Aggregate(query, (current, property) => current.Include(property));
		}

		var sql = query.ToQueryString();
		Debug.WriteLine(sql);

		return query;
	}
}
