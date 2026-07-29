using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Sample.Facade.Behaviors;

internal class RequestInfoBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IRoutedMessage
{
	public Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		return next(context);
	}
}