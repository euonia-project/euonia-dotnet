namespace Nerosoft.Euonia.Repository;

/// <summary>
/// Resolves a connection string for a specific repository context type.
/// </summary>
/// <typeparam name="TContext">
/// The repository context type this resolver targets. Must be a reference type that implements <see cref="IRepositoryContext"/>.
/// </typeparam>
public interface IConnectionStringResolver<TContext>
	where TContext : class, IRepositoryContext
{
	/// <summary>
	/// Asynchronously obtains the connection string for the configured context.
	/// </summary>
	/// <param name="cancellation">
	/// A <see cref="CancellationToken"/> used to cancel the operation. Defaults to <see cref="CancellationToken.None"/>.
	/// </param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> whose result is the resolved connection string.
	/// The result may be <c>null</c> or empty if no connection string could be resolved.
	/// </returns>
	Task<string> GetConnectionStringAsync(CancellationToken cancellation = default);
}