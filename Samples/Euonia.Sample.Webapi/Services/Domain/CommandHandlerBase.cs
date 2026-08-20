using System.Diagnostics.CodeAnalysis;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Provides a base implementation for command handlers with common asynchronous execution helpers.
/// </summary>
/// <remarks>
/// This class supplies the <see cref="Factory"/> and <see cref="Actuator"/> dependencies to subclasses
/// and offers <see cref="ExecuteAsync(Func{Task}, CancellationToken)"/> and
/// <see cref="ExecuteAsync{TResult}(Func{Task{TResult}}, Action{TResult}, CancellationToken)"/>
/// helper methods that centralize how command actions are awaited and how their results are handed off.
/// </remarks>
public abstract class CommandHandlerBase
{
	/// <summary>
	/// Gets the object factory used to create and fetch business objects.
	/// </summary>
	protected IObjectFactory Factory { get; }

	/// <summary>
	/// Gets the actuator used to build operations (for example, create, update, delete) for editable objects.
	/// </summary>
	protected IActuator Actuator { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandHandlerBase"/> class.
	/// </summary>
	/// <param name="factory">The object factory to use for business object operations.</param>
	/// <param name="actuator">The actuator used to build editable-object operations.</param>
	protected CommandHandlerBase(IObjectFactory factory, IActuator actuator)
	{
		Factory = factory;
		Actuator = actuator;
	}

	/// <summary>
	/// Asynchronously executes the specified action.
	/// </summary>
	/// <param name="action">The asynchronous action to execute. This parameter must not be <see langword="null"/>.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous execution.</returns>
	/// <remarks>
	/// The action is awaited and any exception it throws propagates to the caller.
	/// This overload does not produce a result; use
	/// <see cref="ExecuteAsync{TResult}(Func{Task{TResult}}, Action{TResult}, CancellationToken)"/>
	/// when a result must be passed to a continuation.
	/// </remarks>
	protected virtual async Task ExecuteAsync([NotNull] Func<Task> action, CancellationToken cancellationToken = default)
	{
		await action();
	}

	/// <summary>
	/// Asynchronously executes an action that produces a result, then invokes a synchronous continuation with that result.
	/// </summary>
	/// <typeparam name="TResult">The type of the result produced by the action.</typeparam>
	/// <param name="action">The asynchronous action that produces a result. This parameter must not be <see langword="null"/>.</param>
	/// <param name="next">The synchronous continuation to invoke with the produced result. This parameter must not be <see langword="null"/>.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous execution.</returns>
	/// <remarks>
	/// The action is awaited and its result is passed to <paramref name="next"/>.
	/// Exceptions thrown by either the action or the continuation propagate to the caller.
	/// </remarks>
	protected virtual async Task ExecuteAsync<TResult>([NotNull] Func<Task<TResult>> action, Action<TResult> next, CancellationToken cancellationToken = default)
	{
		var result = await action();
		next(result);
	}
}