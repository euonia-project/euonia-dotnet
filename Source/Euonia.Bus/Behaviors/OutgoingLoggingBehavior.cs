using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Behaviors;

internal sealed class OutgoingLoggingBehavior<TMessage, TResult> : IPipelineBehavior<IMessageEnvelope<TMessage>, TResult>
{
	private readonly ILogger _logger;
	private readonly string _transport;

	public OutgoingLoggingBehavior(string transport, ILogger logger)
	{
		_transport = transport;
		_logger = logger;
	}

	public Task<TResult> HandleAsync(IMessageEnvelope<TMessage> context, PipelineDelegate<IMessageEnvelope<TMessage>, TResult> next)
	{
		// Debug 级别记录，避免默认日志级别下每条消息都输出日志。
		_logger?.LogDebug("Message '{Id}'({Type}) transport via '{Transport}' on channel: {Channel}.", context.MessageId, typeof(TMessage).FullName, _transport, context.Channel);
		return next(context);
	}
}