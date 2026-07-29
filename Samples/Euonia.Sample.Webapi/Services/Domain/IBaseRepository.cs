using System.Linq.Expressions;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Defines common asynchronous repository operations for entities.
/// </summary>
/// <typeparam name="TEntity">The entity type handled by the repository.</typeparam>
/// <typeparam name="TKey">The entity primary key type.</typeparam>
public interface IBaseRepository<TEntity, in TKey>
	where TEntity : class, IEntity<TKey>
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// Inserts a new entity into the underlying data store.
	/// </summary>
	/// <param name="entity">The entity instance to insert. Must not be <see langword="null"/>.</param>
	/// <param name="autoSave">
	/// If <see langword="true"/>, the repository will persist changes to the data store
	/// (for example by calling <c>SaveChangesAsync</c>) after the insert operation.
	/// If <see langword="false"/>, the caller is responsible for saving changes.
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>The inserted entity instance including any changes applied by the store (e.g. generated keys).</returns>
	Task<TEntity> InsertAsync(TEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing entity in the underlying data store.
	/// </summary>
	/// <param name="entity">The entity instance containing updated values. Must not be <see langword="null"/>.</param>
	/// <param name="autoSave">
	/// If <see langword="true"/>, the repository will persist changes to the data store
	/// after the update operation. If <see langword="false"/>, the caller is responsible for saving changes.
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	Task UpdateAsync(TEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves an entity by its primary key.
	/// </summary>
	/// <param name="id">The primary key value of the entity to retrieve.</param>
	/// <param name="tracking">
	/// When <see langword="true"/>, the returned entity will be tracked by the underlying context (useful for subsequent updates).
	/// When <see langword="false"/>, the entity will be returned in a no-tracking state for read-only scenarios.
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>The entity instance if found; otherwise implementation-specific (commonly <see langword="null"/> or an exception).</returns>
	Task<TEntity> GetAsync(TKey id, bool tracking, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves an entity by its primary key and includes related navigation properties.
	/// </summary>
	/// <param name="id">The primary key value of the entity to retrieve.</param>
	/// <param name="tracking">
	/// When <see langword="true"/>, the returned entity will be tracked by the underlying context.
	/// When <see langword="false"/>, the entity will be returned in a no-tracking state for read-only scenarios.
	/// </param>
	/// <param name="properties">
	/// An array of related property names to include (e.g. navigation properties). Implementations should ignore unknown names.
	/// Use this to eagerly load related data.
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>The entity instance with requested related data included if found; otherwise implementation-specific.</returns>
	Task<TEntity> GetAsync(TKey id, bool tracking, string[] properties, CancellationToken cancellationToken = default);

	/// <summary>
	/// Determines whether any entity exists that satisfies the specified predicate.
	/// </summary>
	/// <param name="expression">A predicate to test entities against.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns><see langword="true"/> if at least one matching entity exists; otherwise <see langword="false"/>.</returns>
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

	/// <summary>
	/// Counts entities that match the provided predicate and optional query modifications.
	/// </summary>
	/// <param name="expression">A predicate to filter entities. If <see langword="null"/>, counts all entities.</param>
	/// <param name="handle">
	/// A delegate that receives an <see cref="IQueryable{TEntity}"/> and returns an <see cref="IQueryable{TEntity}"/>
	/// allowing callers to apply additional query operators (filters, includes, projections).
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>The number of entities that match the query.</returns>
	Task<int> CountAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds entities that satisfy the specified predicate with additional query customization.
	/// </summary>
	/// <param name="predicate">A predicate used to filter the entities.</param>
	/// <param name="handle">
	/// A delegate to customize the query (for example to add includes, ordering, or additional filters).
	/// The delegate receives the base <see cref="IQueryable{TEntity}"/> and returns the modified query.
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>A list of entities matching the query. Returns an empty list if no entities match.</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds entities that satisfy the specified predicate with pagination and additional query customization.
	/// </summary>
	/// <param name="predicate">A predicate used to filter the entities.</param>
	/// <param name="handle">
	/// A delegate to customize the query (for example to add includes, ordering, or additional filters).
	/// </param>
	/// <param name="offset">Zero-based offset of the first item to return.</param>
	/// <param name="count">Maximum number of items to return.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>A list of entities representing the requested page. Returns an empty list if no entities match.</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the entity identified by the given primary key.
	/// </summary>
	/// <param name="id">The primary key value of the entity to delete.</param>
	/// <param name="autoSave">
	/// If <see langword="true"/>, the repository will persist changes after deletion.
	/// If <see langword="false"/>, the caller must save changes manually.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <exception cref="NotFoundException">Thrown when no entity with the specified key exists (implementation-specific).</exception>
	Task DeleteAsync(TKey id, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the provided entity instance.
	/// </summary>
	/// <param name="entity">The entity instance to delete.</param>
	/// <param name="autoSave">If <see langword="true"/>, persist changes after deletion; otherwise the caller must save changes.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns></returns>
	Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the provided entity instance and raises a domain event produced by <paramref name="eventFactory"/>.
	/// </summary>
	/// <typeparam name="TEvent">The domain event type to be generated and published after deletion.</typeparam>
	/// <param name="entity">The entity instance to delete.</param>
	/// <param name="eventFactory">A factory delegate producing a domain event to be raised for this deletion.</param>
	/// <param name="autoSave">If <see langword="true"/>, persist changes after deletion; otherwise the caller must save changes.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	Task DeleteAsync<TEvent>(TEntity entity, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent;

	/// <summary>
	/// Deletes the entity identified by the given key and raises a domain event produced by <paramref name="eventFactory"/>.
	/// </summary>
	/// <typeparam name="TEvent">The domain event type to be generated and published after deletion.</typeparam>
	/// <param name="id">The primary key value of the entity to delete.</param>
	/// <param name="eventFactory">A factory delegate producing a domain event to be raised for this deletion.</param>
	/// <param name="autoSave">If <see langword="true"/>, persist changes after deletion; otherwise the caller must save changes.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>A task representing the asynchronous delete operation.</returns>
	Task DeleteAsync<TEvent>(TKey id, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent;

	/// <summary>
	/// Finds entities using a query customization delegate.
	/// </summary>
	/// <param name="handle">
	/// A delegate that receives an <see cref="IQueryable{TEntity}"/> and returns an <see cref="IQueryable{TEntity}"/>
	/// with the desired modifications (includes, ordering, filtering, projection to entity).
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>A list of entities returned by the customized query. Returns an empty list if none found.</returns>
	Task<List<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Counts entities based on a customizable query delegate.
	/// </summary>
	/// <param name="handle">A delegate that configures the query used to count entities.</param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>The number of entities produced by the configured query.</returns>
	Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes a customized query expected to return a single entity.
	/// </summary>
	/// <param name="handle">
	/// A delegate that configures the query (for example to add filters and includes) and should result in a single entity.
	/// Implementations should document whether they return the first matching entity or throw when multiple/none are found.
	/// </param>
	/// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
	/// <returns>The single entity returned by the configured query; behavior on zero or multiple results is implementation-specific.</returns>
	Task<TEntity> GetAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);
}
