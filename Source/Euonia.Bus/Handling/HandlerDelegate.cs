namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示特定类型消息的处理委托，不返回结果。
/// </summary>
/// <typeparam name="TMessage">消息的类型，必须是引用类型。</typeparam>
/// <param name="message">待处理的消息实例。</param>
/// <param name="context">消息上下文。</param>
/// <param name="cancellationToken">用于取消处理操作的令牌。</param>
/// <returns>表示异步处理操作的任务。</returns>
public delegate Task HandlerDelegate<in TMessage>(TMessage message, IMessageContext context, CancellationToken cancellationToken = default)
	where TMessage : class;

/// <summary>
/// 表示消息的通用处理委托，返回处理结果。
/// </summary>
/// <param name="message">待处理的消息实例。</param>
/// <param name="context">消息上下文。</param>
/// <param name="cancellationToken">用于取消处理操作的令牌。</param>
/// <returns>包含处理结果的任务。</returns>
public delegate Task<object> HandlerDelegate(object message, IMessageContext context, CancellationToken cancellationToken = default);