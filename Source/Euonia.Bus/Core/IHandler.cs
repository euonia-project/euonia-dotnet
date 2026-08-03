namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息处理程序的协定。
/// </summary>
public interface IHandler
{
	/*
	/// <summary>
	/// 判断当前消息处理程序能否处理指定类型的消息。
	/// </summary>
	/// <param name="messageType">要检查的消息类型。</param>
	/// <returns>如果当前消息处理程序可以处理指定类型的消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool CanHandle(Type messageType);
	*/
}

/// <summary>
/// 定义处理特定类型消息并返回响应的协定。
/// </summary>
/// <typeparam name="TMessage">消息的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
public interface IHandler<in TMessage, TResponse> : IHandler
{
	/// <summary>
	/// 处理消息。
	/// </summary>
	/// <param name="message">消息实例。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	Task<TResponse> HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// 消息处理程序的协定（无返回值）。
/// </summary>
/// <typeparam name="TMessage">消息的类型。</typeparam>
public interface IHandler<in TMessage> : IHandler<TMessage, Unit>
	where TMessage : class
{
	/// <summary>
	/// 处理消息。
	/// </summary>
	/// <param name="message">消息实例。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	new Task HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken = default);

	/// <summary>
	/// 显式实现 <see cref="IHandler{TMessage,Unit}"/> 的 <c>HandleAsync</c> 方法，
	/// 调用本接口的 <c>HandleAsync</c> 方法并将异步处理结果映射为 <see cref="Unit"/>。
	/// </summary>
	/// <param name="message">消息实例。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步操作的任务，完成后的结果为 <see cref="Unit.Value"/>。</returns>
	Task<Unit> IHandler<TMessage, Unit>.HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken)
	{
		return HandleAsync(message, context, cancellationToken).ContinueWith(_ => Unit.Value, cancellationToken);
	}
}