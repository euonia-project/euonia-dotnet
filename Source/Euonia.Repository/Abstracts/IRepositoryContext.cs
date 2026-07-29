using System.Data;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// The interface of repository context.
/// </summary>
public interface IRepositoryContext : IDisposable
{
	/// <summary>
	/// Gets the identifier.
	/// </summary>
	/// <value>The identifier.</value>
	Guid Id { get; }

	/// <summary>
	/// Gets the context provider.
	/// </summary>
	string Provider { get; }

	/// <summary>
	/// Creates a <see cref="T:IQueryable`1" /> that can be used to query and save instances of <typeparamref name="TEntity" />.
	/// </summary>
	/// <typeparam name="TEntity"> The type of entity for which a set should be returned. </typeparam>
	/// <returns> A set for the given entity type. </returns>
	IQueryable<TEntity> SetOf<TEntity>() where TEntity : class;

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	IDbConnection GetConnection();

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	IDbTransaction GetTransaction();

	/// <summary>
	/// Commit changes async.
	/// </summary>
	/// <returns>The async.</returns>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Commits the changes asynchronously.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task CommitAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task RollbackAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the tracked entries.
	/// </summary>
	/// <returns></returns>
	IEnumerable<object> GetTrackedEntries();
}