using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Threading;
// ReSharper disable AccessToDisposedClosure

namespace Nerosoft.Euonia.Uow;

/// <inheritdoc />
public class UnitOfWorkInterceptor : IInterceptor
{
	private readonly IServiceScopeFactory _factory;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWorkInterceptor"/> class.
	/// </summary>
	/// <param name="factory"></param>
	public UnitOfWorkInterceptor(IServiceScopeFactory factory)
	{
		_factory = factory;
	}

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		if (!UnitOfWorkHelper.IsUnitOfWorkMethod(invocation.Method, out var attribute))
		{
			invocation.Proceed();
		}
		else
		{
			using var scope = _factory.CreateScope();
			var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

			var isTransactional = PriorityValueFinder.Find<bool?>(queue =>
			{
				queue.Enqueue(() => attribute.IsTransactional, 1);
				queue.Enqueue(() => scope.ServiceProvider.GetService<IOptions<UnitOfWorkOptions>>()?.Value.IsTransactional, 2);
			}, t => t.HasValue) ?? false;

			var timeout = PriorityValueFinder.Find<TimeSpan?>(queue =>
			{
				queue.Enqueue(() => attribute.Timeout, 1);
				queue.Enqueue(() => scope.ServiceProvider.GetService<IOptions<UnitOfWorkOptions>>()?.Value.Timeout, 2);
			}, t => t.HasValue);

			using var uow = manager.Begin(isTransactional);
			invocation.Proceed();

			var cancellationToken = timeout.HasValue ? new CancellationTokenSource(timeout.Value).Token : CancellationToken.None;

			AsyncContext.Run(() => uow.CompleteAsync(cancellationToken));
		}
	}
}