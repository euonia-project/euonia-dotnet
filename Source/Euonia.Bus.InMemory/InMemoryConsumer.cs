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
}