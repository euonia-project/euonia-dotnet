using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 基于内存的请求执行器实现，用于处理请求-响应模式的消息。
/// </summary>
public class InMemoryExecutor : InMemoryRecipient<InMemoryExecutor>, IExecutor
{
	/// <summary>
	/// 初始化 <see cref="InMemoryExecutor"/> 类的新实例。
	/// </summary>
	/// <param name="handler">用于业务处理的消息处理器上下文。</param>
	/// <param name="factory">用于创建类型化日志记录器的日志工厂。</param>
	public InMemoryExecutor(IHandlerContext handler, ILoggerFactory factory)
		: base(handler, factory)
	{
	}

	/// <summary>
	/// 获取接收者的名称。
	/// </summary>
	public override string Name => nameof(InMemoryExecutor);
}