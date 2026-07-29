using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Concrete implementation of <see cref="IUnitOfWork"/> that manages scoped contexts,
/// commit/rollback lifecycles and completion handlers.
/// </summary>
public sealed class UnitOfWork : UnitOfWorkBase, IUnitOfWork
{
	#region Events

	/// <summary>
	/// Event fired when this unit of work has been completed successfully.
	/// </summary>
	public event EventHandler<UnitOfWorkEventArgs> Completed;

	/// <summary>
	/// Event fired when this unit of work has failed.
	/// </summary>
	public event EventHandler<UnitOfWorkFailedEventArgs> Failed;

	#endregion

	#region Fields

	/// <summary>
	/// Default options sourced from the options monitor to be normalized against provided options.
	/// </summary>
	private readonly UnitOfWorkOptions _defaultOptions;

	/// <summary>
	/// Thread-safe storage of registered <see cref="IUnitOfWorkContext"/> instances keyed by name.
	/// </summary>
	private readonly ConcurrentDictionary<string, IUnitOfWorkContext> _contexts = new();

	/// <summary>
	/// Indicates whether completion is currently in progress to prevent reentrancy.
	/// </summary>
	private bool _isCompleting;

	#endregion

	#region Ctors

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWork"/> class.
	/// </summary>
	/// <param name="provider">The service provider scoped to this unit of work.</param>
	/// <param name="options">Options monitor providing default unit of work options.</param>
	public UnitOfWork(IServiceProvider provider, IOptionsMonitor<UnitOfWorkOptions> options)
	{
		ServiceProvider = provider;
		_defaultOptions = options.CurrentValue;
	}

	#endregion

	#region Properties of IUnitOfWork

	/// <inheritdoc />
	/// <summary>
	/// Gets the unique identifier for this unit of work instance.
	/// </summary>
	public Guid Id { get; } = Guid.NewGuid();

	/// <inheritdoc />
	/// <summary>
	/// Gets a mutable dictionary for storing arbitrary contextual data for this unit of work.
	/// </summary>
	public Dictionary<string, object> Items { get; } = new();

	/// <inheritdoc />
	/// <summary>
	/// Gets a read-only view of the registered <see cref="IUnitOfWorkContext"/> instances.
	/// </summary>
	public IReadOnlyDictionary<string, IUnitOfWorkContext> Contexts => _contexts;

	/// <summary>
	/// Gets the <see cref="IServiceProvider"/> scoped to this unit of work.
	/// </summary>
	public override IServiceProvider ServiceProvider { get; }

	/// <inheritdoc />
	/// <summary>
	/// Gets the outer (parent) unit of work if this unit is nested; otherwise null.
	/// </summary>
	public IUnitOfWork Outer { get; private set; }

	/// <inheritdoc />
	/// <summary>
	/// Gets a value indicating whether this unit of work has been reserved for exclusive use.
	/// </summary>
	public bool IsReserved { get; private set; }

	/// <summary>
	/// Gets the reservation name when this instance has been reserved.
	/// </summary>
	public string ReservationName { get; private set; }

	/// <summary>
	/// Gets a value indicating whether this instance has been disposed.
	/// </summary>
	public bool IsDisposed { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the unit of work has been completed.
	/// </summary>
	public bool IsCompleted { get; private set; }

	#endregion

	#region Properties of self

	/// <summary>
	/// Handlers to be invoked when the unit of work completes successfully.
	/// </summary>
	private List<Func<Task>> CompletedHandlers { get; } = new();

	/// <summary>
	/// The effective options applied to this unit of work after initialization.
	/// </summary>
	public IUnitOfWorkOptions Options { get; private set; }

	/// <summary>
	/// Captured exception if the unit of work fails during completion.
	/// </summary>
	private Exception Exception { get; set; }

	/// <summary>
	/// Indicates whether the unit of work has been rolled back.
	/// </summary>
	private bool RolledBack { get; set; }

	#endregion

	#region Methods of IUnitOfWork

	/// <summary>
	/// Registers an asynchronous handler to be executed after a successful completion.
	/// </summary>
	/// <param name="handler">The asynchronous handler to register.</param>
	public void OnCompleted(Func<Task> handler)
	{
		CompletedHandlers.Add(handler);
	}

	/// <summary>
	/// Initializes the unit of work with the provided options. Options are normalized
	/// against defaults from the options monitor.
	/// </summary>
	/// <param name="options">Options to configure this unit of work.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
	/// <exception cref="Exception">Thrown if the unit of work has already been initialized.</exception>
	public void Initialize(UnitOfWorkOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (Options != null)
		{
			throw new Exception("This unit of work is already initialized before!");
		}

		Options = _defaultOptions.Normalize(options);

		IsReserved = false;
	}

	/// <inheritdoc />
	/// <summary>
	/// Marks this unit of work as reserved using the given name.
	/// </summary>
	/// <param name="reservationName">The reservation identifier to assign.</param>
	public void Reserve(string reservationName)
	{
		Check.EnsureNotNull(reservationName, nameof(reservationName));

		ReservationName = reservationName;
		IsReserved = true;
	}

	/// <inheritdoc />
	/// <summary>
	/// Finds a registered context by key.
	/// </summary>
	/// <param name="key">The context key to find.</param>
	/// <returns>The found <see cref="IUnitOfWorkContext"/> or <c>null</c> if not present.</returns>
	public IUnitOfWorkContext FindContext(string key)
	{
		return _contexts.GetOrDefault(key);
	}

	/// <inheritdoc />
	/// <summary>
	/// Adds a context instance under the specified key.
	/// </summary>
	/// <param name="key">The context key.</param>
	/// <param name="context">The context instance to add.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when a context with the given key already exists or could not be added.</exception>
	public void AddContext(string key, IUnitOfWorkContext context)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentNullException.ThrowIfNull(context);

		if (_contexts.ContainsKey(key))
		{
			throw new InvalidOperationException("This unit of work already already contains a context with the key: " + key);
		}

		if (!_contexts.TryAdd(key, context))
		{
			throw new InvalidOperationException("Failed to add context with the key: " + key);
		}
	}

	/// <inheritdoc />
	/// <summary>
	/// Gets an existing context by key or adds a new one created by the provided factory.
	/// </summary>
	/// <param name="key">The context key.</param>
	/// <param name="factory">Factory used to create the context if it does not exist.</param>
	/// <returns>The existing or newly created <see cref="IUnitOfWorkContext"/> instance.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
	public IUnitOfWorkContext GetOrAddContext(string key, Func<IUnitOfWorkContext> factory)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentNullException.ThrowIfNull(factory);

		return _contexts.GetOrAdd(key, _ => factory());
	}

	/// <summary>
	/// Sets the parent (outer) unit of work for nesting scenarios.
	/// </summary>
	/// <param name="outer">The outer <see cref="IUnitOfWork"/> instance.</param>
	public void SetOuter(IUnitOfWork outer)
	{
		Outer = outer;
	}

	/// <summary>
	/// Completes the unit of work by saving changes on all contexts and invoking completion handlers.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token to observe.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="InvalidOperationException">If completion has already been requested.</exception>
	public async Task CompleteAsync(CancellationToken cancellationToken = default)
	{
		if (RolledBack)
		{
			return;
		}

		if (IsCompleted || _isCompleting)
		{
			throw new InvalidOperationException("Completion has already been requested for this unit of work.");
		}

		try
		{
			_isCompleting = true;
			await SaveChangesAsync(cancellationToken);
			IsCompleted = true;
			await OnCompletedAsync();
		}
		catch (Exception exception)
		{
			Exception = exception;
			throw;
		}
	}

	/// <inheritdoc />
	/// <summary>
	/// Persists pending changes for all registered contexts in this unit of work.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token to observe.</param>
	/// <returns>A task representing the asynchronous save operation.</returns>
	public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		if (RolledBack)
		{
			return;
		}

		foreach (var (_, context) in Contexts)
		{
			await context.SaveChangesAsync(cancellationToken);
		}
	}

	/// <summary>
	/// Invokes registered completion handlers and raises the <see cref="Completed"/> event.
	/// </summary>
	private async Task OnCompletedAsync()
	{
		foreach (var handler in CompletedHandlers)
		{
			await handler.Invoke();
		}

		Completed?.Invoke(this, new UnitOfWorkEventArgs(this));
	}

	/// <inheritdoc />
	/// <summary>
	/// Rolls back all registered contexts.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token to observe.</param>
	/// <returns>A task representing the asynchronous rollback operation.</returns>
	public async Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		if (RolledBack)
		{
			return;
		}

		RolledBack = true;

		foreach (var (_, context) in Contexts)
		{
			await context.RollbackAsync(cancellationToken);
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	/// <summary>
	/// Releases managed resources held by the unit of work, raises failure event when necessary,
	/// and invokes disposal notifications.
	/// </summary>
	/// <param name="disposing">Indicates whether the method was called from Dispose.</param>
	protected override void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}

		IsDisposed = true;

		foreach (var (_, _) in Contexts)
		{
			//context.Dispose();
		}

		if (!IsCompleted || Exception != null)
		{
			OnFailed();
		}

		InvokeDisposedEvent(this, new DisposedEventArgs());
	}

	#endregion

	#region Methods of self

	/// <summary>
	/// Raises the <see cref="Failed"/> event with the captured exception and rollback state.
	/// </summary>
	private void OnFailed()
	{
		Failed?.Invoke(this, new UnitOfWorkFailedEventArgs(this, Exception, RolledBack));
	}

	#endregion
}