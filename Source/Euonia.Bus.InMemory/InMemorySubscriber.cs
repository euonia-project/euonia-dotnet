using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 基于内存的主题订阅者实现，用于接收和处理多播消息。
/// </summary>
public class InMemorySubscriber : InMemoryRecipient<InMemorySubscriber>, ISubscriber
{
	/// <summary>
	/// 初始化 <see cref="InMemorySubscriber"/> 类的新实例。
	/// </summary>
	/// <param name="handler">用于业务处理的消息处理器上下文。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public InMemorySubscriber(IHandlerContext handler, ILoggerFactory logger)
		: base(handler, logger)
	{
	}
	
	/// <summary>
	/// 获取接收者的名称。
	/// </summary>
	public override string Name => nameof(InMemorySubscriber);
}