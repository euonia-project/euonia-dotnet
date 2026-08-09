using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Behaviors;

internal sealed class OutgoingLoggingBehavior<TMessage, TResult> : IPipelineBehavior<IMessageEnvelope<TMessage>, TResult>
{
	private readonly ILogger<OutgoingLoggingBehavior<TMessage, TResult>> _logger;
	private readonly string _transport;

	public OutgoingLoggingBehavior(string transport, ILoggerFactory logger)
	{
		_transport = transport;
		_logger = logger.CreateLogger<OutgoingLoggingBehavior<TMessage, TResult>>();
	}

	public Task<TResult> HandleAsync(IMessageEnvelope<TMessage> context, PipelineDelegate<IMessageEnvelope<TMessage>, TResult> next)
	{
		_logger.LogInformation("Message '{Id}'({Type}) transport via '{Transport}' on channel: {Channel}.", context.MessageId, typeof(TMessage).FullName, _transport, context.Channel);
		return next(context);
	}
}