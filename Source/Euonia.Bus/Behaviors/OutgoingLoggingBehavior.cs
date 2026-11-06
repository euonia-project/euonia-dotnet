using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Behaviors;

/// <summary>
/// 出站消息管道行为：在消息交给传输器前按 Debug 级别记录消息标识、类型、传输器与通道。
/// </summary>
internal sealed class OutgoingLoggingBehavior<TMessage, TResult> : IPipelineBehavior<IMessageEnvelope<TMessage>, TResult>
{
	private readonly ILogger _logger;
	private readonly string _transport;

	/// <summary>
	/// 初始化 <see cref="OutgoingLoggingBehavior{TMessage, TResult}"/> 的新实例。
	/// </summary>
	/// <param name="transport">当前传输器名称，仅用于日志输出。</param>
	/// <param name="logger">用于写入出站日志的记录器。</param>
	public OutgoingLoggingBehavior(string transport, ILogger logger)
	{
		_transport = transport;
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<TResult> HandleAsync(IMessageEnvelope<TMessage> context, PipelineDelegate<IMessageEnvelope<TMessage>, TResult> next)
	{
		// Debug 级别记录，避免默认日志级别下每条消息都输出日志。
		_logger?.LogDebug("Message '{Id}'({Type}) transport via '{Transport}' on channel: {Channel}.", context.MessageId, typeof(TMessage).FullName, _transport, context.Channel);
		return next(context);
	}
}