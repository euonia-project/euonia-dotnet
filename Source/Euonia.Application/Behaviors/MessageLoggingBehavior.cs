using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Application;

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
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public MessageLoggingBehavior(ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<MessageLoggingBehavior<TMessage, TResponse>>();
	}

	/// <summary>
	/// 记录消息的 ID 和完整类型名，然后调用下一个管道委托。
	/// </summary>
	/// <param name="context">正在处理的消息信封。</param>
	/// <param name="next">下一个要调用的管道委托。</param>
	/// <returns>包含管道响应结果的任务。</returns>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		_logger.LogInformation("Message {Id} - {FullName}: {Context}", context.MessageId, context.GetType().FullName, context);
		return await next(context);
	}
}