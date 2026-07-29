namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Represents a unit of work scope. Manages lifetime, contexts, service resolution,
/// and the commit/rollback lifecycle for a logical operation.
/// </summary>
public interface IUnitOfWork : IDisposable
{
	/// <summary>
	/// Occurs when the unit of work is disposed.
	/// </summary>
	event EventHandler<DisposedEventArgs> Disposed;

	/// <summary>
	/// Occurs when the unit of work has been completed successfully.
	/// </summary>
	event EventHandler<UnitOfWorkEventArgs> Completed;

	/// <summary>
	/// Occurs when the unit of work has failed.
	/// </summary>
	event EventHandler<UnitOfWorkFailedEventArgs> Failed;

	/// <summary>
	/// Gets the unique identifier of this unit of work instance.
	/// </summary>
	Guid Id { get; }

	/// <summary>
	/// Gets a mutable dictionary to store arbitrary contextual data for this unit of work.
	/// </summary>
	Dictionary<string, object> Items { get; }

	/// <summary>
	/// Gets the configuration options used by this unit of work.
	/// </summary>
	IUnitOfWorkOptions Options { get; }

	/// <summary>
	/// Gets the read-only collection of registered <see cref="IUnitOfWorkContext"/> instances,
	/// keyed by a string identifier.
	/// </summary>
	IReadOnlyDictionary<string, IUnitOfWorkContext> Contexts { get; }

	/// <summary>
	/// Gets the <see cref="IServiceProvider"/> scoped to this unit of work.
	/// Use this to resolve services with the unit of work's lifetime.
	/// </summary>
	IServiceProvider ServiceProvider { get; }

	/// <summary>
	/// Gets the outer (parent) unit of work when this unit of work is nested; otherwise null.
	/// </summary>
	IUnitOfWork Outer { get; }

	/// <summary>
	/// Gets a value indicating whether this unit of work has been reserved.
	/// </summary>
	bool IsReserved { get; }

	/// <summary>
	/// Gets the reservation name if this unit of work is reserved; otherwise null.
	/// </summary>
	string ReservationName { get; }

	/// <summary>
	/// Gets a value indicating whether this unit of work has been disposed.
	/// </summary>
	bool IsDisposed { get; }

	/// <summary>
	/// Gets a value indicating whether this unit of work has been completed.
	/// </summary>
	bool IsCompleted { get; }

	/// <summary>
	/// Persists pending changes for all registered contexts within this unit of work.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous save operation.</returns>
	Task SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Rolls back any pending operations in this unit of work.
	/// Implementations should ensure contexts are returned to a consistent state.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous rollback operation.</returns>
	Task RollbackAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Completes the unit of work, committing changes as appropriate and triggering completion events.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous completion operation.</returns>
	Task CompleteAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Registers an asynchronous handler to be executed when the unit of work is completed.
	/// </summary>
	/// <param name="handler">The handler to execute on completion.</param>
	void OnCompleted(Func<Task> handler);

	/// <summary>
	/// Sets the outer (parent) unit of work for this instance. Used for nested unit of work scenarios.
	/// </summary>
	/// <param name="outer">The parent <see cref="IUnitOfWork"/>.</param>
	void SetOuter(IUnitOfWork outer);

	/// <summary>
	/// Initializes this unit of work with the provided options.
	/// </summary>
	/// <param name="options">The options to apply to this unit of work.</param>
	void Initialize(UnitOfWorkOptions options);

	/// <summary>
	/// Reserves this unit of work with a specified name, marking it for exclusive use.
	/// </summary>
	/// <param name="reservationName">The reservation identifier.</param>
	void Reserve(string reservationName);

	/// <summary>
	/// Resolves a service of type <typeparamref name="TService"/> from the unit of work's scope.
	/// </summary>
	/// <typeparam name="TService">The service type to resolve.</typeparam>
	/// <returns>An instance of <typeparamref name="TService"/> or null if not registered.</returns>
	TService GetService<TService>() where TService : class;

	/// <summary>
	/// Resolves all services of type <typeparamref name="TService"/> from the unit of work's scope.
	/// </summary>
	/// <typeparam name="TService">The service type to resolve.</typeparam>
	/// <returns>A sequence of resolved services; empty if none registered.</returns>
	IEnumerable<TService> GetServices<TService>() where TService : class;

	/// <summary>
	/// Resolves a service of the specified <paramref name="serviceType"/> from the unit of work's scope.
	/// </summary>
	/// <param name="serviceType">The service type to resolve.</param>
	/// <returns>An instance of the service or null if not registered.</returns>
	object GetService(Type serviceType);

	/// <summary>
	/// Resolves all services of the specified <paramref name="serviceType"/> from the unit of work's scope.
	/// </summary>
	/// <param name="serviceType">The service type to resolve.</param>
	/// <returns>A sequence of resolved services; empty if none registered.</returns>
	IEnumerable<object> GetServices(Type serviceType);

	/// <summary>
	/// Finds a registered <see cref="IUnitOfWorkContext"/> by its key.
	/// </summary>
	/// <param name="key">The context key to find.</param>
	/// <returns>The <see cref="IUnitOfWorkContext"/> if found; otherwise null.</returns>
	IUnitOfWorkContext FindContext(string key);

	/// <summary>
	/// Adds the specified <see cref="IUnitOfWorkContext"/> to this unit of work using the provided key.
	/// </summary>
	/// <param name="key">The key to register the context under.</param>
	/// <param name="context">The context instance to add.</param>
	void AddContext(string key, IUnitOfWorkContext context);

	/// <summary>
	/// Gets an existing <see cref="IUnitOfWorkContext"/> by key or adds a new one created by the provided factory.
	/// </summary>
	/// <param name="key">The context key to get or add.</param>
	/// <param name="factory">A factory that creates the context if it does not exist.</param>
	/// <returns>The existing or newly created <see cref="IUnitOfWorkContext"/> instance.</returns>
	IUnitOfWorkContext GetOrAddContext(string key, Func<IUnitOfWorkContext> factory);
}