using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 基于内存的队列消费者实现，用于接收和处理单播消息。
/// 处理完成后会调用 <see cref="MessageContext.Complete(string)"/>，异常时调用 <see cref="MessageContext.Failure"/>。
/// </summary>
public class InMemoryConsumer : InMemoryRecipient<InMemoryConsumer>, IConsumer
{
	/// <summary>
	/// 初始化 <see cref="InMemoryConsumer"/> 类的新实例。
	/// </summary>
	/// <param name="handler">用于业务处理的消息处理器上下文。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public InMemoryConsumer(IHandlerContext handler, ILoggerFactory logger)
		: base(handler, logger)
	{
	}

	
	/// <summary>
	/// 获取接收者的名称。
	/// </summary>
	public override string Name => nameof(InMemoryConsumer);

	
	/// <summary>
	/// 处理接收到的消息，委托给 <see cref="IHandlerContext"/> 执行业务逻辑。
	/// 异常时会记录错误日志并通知 <see cref="MessageContext.Failure"/>，最终总是调用 <see cref="MessageContext.Complete(string)"/>。
	/// </summary>
	/// <param name="channel">消息通道。</param>
	/// <param name="message">消息负载。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示消息处理异步操作的任务。</returns>
	protected override async Task HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			await Handler.HandleAsync(channel, message, context, cancellationToken);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Message '{Id}' Handle Error: {Message}", context.MessageId, exception.Message);
			context.Failure(exception);
		}
		finally
		{
			context.Complete(null);
		}
	}
}