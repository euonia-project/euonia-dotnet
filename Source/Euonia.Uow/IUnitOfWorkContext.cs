namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Represents an execution context for a unit of work.
/// Provides operations to persist changes and to control completion (commit or rollback).
/// Implementations are expected to release any held resources when disposed.
/// </summary>
public interface IUnitOfWorkContext : IDisposable
{
	/// <summary>
	/// Persists any pending changes within the current unit of work.
	/// This operation does not necessarily finalize the unit of work transactionally;
	/// use <see cref="CommitAsync(CancellationToken)"/> to complete the unit of work.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous save operation.</returns>
	Task SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Commits the unit of work, making all operations permanent (for example, committing a transaction).
	/// After a successful commit the unit of work is considered completed.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous commit operation.</returns>
	Task CommitAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Rolls back the unit of work, undoing any pending operations (for example, rolling back a transaction).
	/// Implementations should ensure the system remains in a consistent state after rollback.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous rollback operation.</returns>
	Task RollbackAsync(CancellationToken cancellationToken = default);
}