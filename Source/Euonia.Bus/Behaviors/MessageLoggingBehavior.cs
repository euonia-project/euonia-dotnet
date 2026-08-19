using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 记录路由消息的日志管道行为。
/// </summary>
/// <typeparam name="TMessage">由管道处理的路由消息类型，必须是实现了 <see cref="IMessageEnvelope"/> 接口的类。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
public sealed class MessageLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private readonly ILogger<MessageLoggingBehavior<TMessage, TResponse>> _logger;

	/// <summary>
	/// 初始化 <see cref="MessageLoggingBehavior{TMessage, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="logger">类型化日志记录器。</param>
	public MessageLoggingBehavior(ILogger<MessageLoggingBehavior<TMessage, TResponse>> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// 记录消息的 ID 和完整类型名，然后调用下一个管道委托。
	/// </summary>
	/// <param name="context">正在处理的消息信封。</param>
	/// <param name="next">下一个要调用的管道委托。</param>
	/// <returns>包含管道响应结果的任务。</returns>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		// Debug 级别记录，避免默认日志级别下每条消息都输出日志；
		// 不记录消息体，防止敏感数据（PII）进入日志。
		_logger.LogDebug("Message {Id} - {FullName}", context.MessageId, context.GetType().FullName);
		return await next(context);
	}
}
