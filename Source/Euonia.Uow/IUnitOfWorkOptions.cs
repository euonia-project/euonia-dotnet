using System.Data;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Represents configuration options for a unit of work.
/// </summary>
public interface IUnitOfWorkOptions
{
	/// <summary>
	/// Gets a value indicating whether the unit of work should be executed within a transaction.
	/// </summary>
	bool IsTransactional { get; }

	/// <summary>
	/// Gets the isolation level to use for the transaction, if any.
	/// </summary>
	/// <remarks>
	/// A <see langword="null"/> value indicates that no specific isolation level was requested
	/// and the default for the data store should be used.
	/// </remarks>
	IsolationLevel? IsolationLevel { get; }

	/// <summary>
	/// Gets the timeout duration for the unit of work, if any.
	/// </summary>
	/// <remarks>
	/// A <see langword="null"/> value indicates no explicit timeout is set and the system/default
	/// timeout should apply.
	/// </remarks>
	TimeSpan? Timeout { get; }
}