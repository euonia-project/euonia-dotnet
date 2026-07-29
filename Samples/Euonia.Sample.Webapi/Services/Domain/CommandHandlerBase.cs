using System.Diagnostics.CodeAnalysis;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Base class for command handlers that encapsulates common execution patterns.
/// </summary>
/// <remarks>
/// This class provides helper methods to execute asynchronous command actions
/// within a unit-of-work scope and to produce <see cref="CommandResponse"/> results.
/// Subclasses should use the provided <see cref="ExecuteAsync(Func{Task}, CancellationToken)"/>,
/// <see cref="ExecuteAsync{TResult}(Func{Task{TResult}}, Action{TResult}, CancellationToken)"/>, or other overloads
/// to ensure consistent transaction handling and error reporting.
/// </remarks>
public abstract class CommandHandlerBase
{
	/// <summary>
	/// Gets the unit-of-work manager used to begin and manage transactional scopes.
	/// </summary>
	/// <remarks>
	/// Implementations expect this property to be initialized by the constructor.
	/// The property is protected and virtual to allow test or derived-class overrides.
	/// </remarks>
	protected virtual IUnitOfWorkManager UnitOfWork { get; }

	/// <summary>
	/// Gets the business object factory used to create domain objects or services.
	/// </summary>
	/// <remarks>
	/// This property may be null if not supplied to the constructor. It is protected
	/// and virtual so derived classes can use or override it.
	/// </remarks>
	protected virtual IObjectFactory Factory { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandHandlerBase"/> class
	/// with the specified unit-of-work manager.
	/// </summary>
	/// <param name="unitOfWork">The unit-of-work manager to use for transactional operations.</param>
	protected CommandHandlerBase(IUnitOfWorkManager unitOfWork)
	{
		UnitOfWork = unitOfWork;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandHandlerBase"/> class
	/// with the specified unit-of-work manager and object factory.
	/// </summary>
	/// <param name="unitOfWork">The unit-of-work manager to use for transactional operations.</param>
	/// <param name="factory">The business object factory used by the handler.</param>
	protected CommandHandlerBase(IUnitOfWorkManager unitOfWork, IObjectFactory factory)
		: this(unitOfWork)
	{
		Factory = factory;
	}

	/// <summary>
	/// Executes an asynchronous operation within a unit-of-work scope.
	/// </summary>
	/// <param name="action">The asynchronous action to execute. This parameter must not be <see langword="null"/>.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>A <see cref="Task"/> representing the asynchronous execution.</returns>
	/// <remarks>
	/// This overload does not wrap the result in a <see cref="CommandResponse"/>; it will
	/// begin a unit-of-work, invoke the action, and commit the transaction.
	/// Exceptions will propagate to the caller.
	/// </remarks>
	protected virtual async Task ExecuteAsync([NotNull] Func<Task> action, CancellationToken cancellationToken = default)
	{
		using var uow = UnitOfWork.Begin(true, true);
		await action();
		await uow.CompleteAsync(cancellationToken);
	}

	/// <summary>
	/// Executes an asynchronous operation that produces a result within a unit-of-work scope,
	/// then invokes a synchronous continuation with the result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result produced by the action.</typeparam>
	/// <param name="action">The asynchronous action that produces a result. This parameter must not be <see langword="null"/>.</param>
	/// <param name="next">A synchronous continuation to run after the unit-of-work is committed. This parameter must not be <see langword="null"/>.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>A <see cref="Task"/> representing the asynchronous execution.</returns>
	/// <remarks>
	/// The action is executed and the returned result is passed to <paramref name="next"/>
	/// after the unit-of-work commits. Exceptions thrown by the action or the continuation
	/// propagate to the caller.
	/// </remarks>
	protected virtual async Task ExecuteAsync<TResult>([NotNull] Func<Task<TResult>> action, Action<TResult> next, CancellationToken cancellationToken = default)
	{
		using var uow = UnitOfWork.Begin(true, true);
		var result = await action();
		await uow.CompleteAsync(cancellationToken);
		next(result);
	}
}