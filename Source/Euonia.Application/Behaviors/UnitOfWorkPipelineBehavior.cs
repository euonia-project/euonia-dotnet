using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// Pipeline behavior that creates an <see cref="IServiceScope"/> and a unit of work for each message.
/// </summary>
/// <typeparam name="TMessage">The routed message type handled by the pipeline.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
/// <remarks>
/// For each invocation the behavior:
///  - creates a scoped dependency injection scope,
///  - resolves <see cref="IUnitOfWorkManager"/>,
///  - begins a unit of work (non-transactional),
///  - invokes the next pipeline delegate,
///  - completes the unit of work and disposes scope and unit of work.
/// </remarks>
public class UnitOfWorkPipelineBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IRoutedMessage
{
	private readonly IServiceScopeFactory _factory;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWorkPipelineBehavior{TMessage, TResponse}"/> class.
	/// </summary>
	/// <param name="factory">The service scope factory used to create a scoped <see cref="IServiceProvider"/> for each message.</param>
	public UnitOfWorkPipelineBehavior(IServiceScopeFactory factory)
	{
		_factory = factory;
	}

	/// <summary>
	/// Handles the pipeline invocation by creating a scope and unit of work, invoking the next delegate, and completing the unit of work.
	/// </summary>
	/// <param name="context">The routed message being processed.</param>
	/// <param name="next">The next pipeline delegate to invoke.</param>
	/// <returns>
	/// A <see cref="Task{TResult}"/> that completes with the response produced by the pipeline.
	/// The unit of work is completed before the task returns.
	/// </returns>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		using var scope = _factory.CreateScope();
		var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
		using var uow = manager.Begin(isTransactional: false);
		var response = await next(context);
		await uow.CompleteAsync(CancellationToken.None);
		return response;
	}
}