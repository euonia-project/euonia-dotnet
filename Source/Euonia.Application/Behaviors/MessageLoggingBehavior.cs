using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// Logs the routed messages.
/// </summary>
public sealed class MessageLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IRoutedMessage
{
	private readonly ILogger<MessageLoggingBehavior<TMessage, TResponse>> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageLoggingBehavior{TMessage, TResponse}"/> class.
	/// </summary>
	/// <param name="logger">The logger service factory.</param>
	public MessageLoggingBehavior(ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<MessageLoggingBehavior<TMessage, TResponse>>();
	}

	/// <inheritdoc />
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		_logger.LogInformation("Message {Id} - {FullName}: {Context}", context.MessageId, context.GetType().FullName, context);
		return await next(context);
	}
}