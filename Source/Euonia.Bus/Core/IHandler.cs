namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义处理特定类型消息并返回响应的协定。
/// </summary>
/// <typeparam name="TMessage">消息的类型。</typeparam>
/// <typeparam name="TResult">响应的类型。</typeparam>
public interface IHandler<in TMessage, TResult>
	where TMessage : class
{
	/// <summary>
	/// 处理消息。
	/// </summary>
	/// <param name="message">消息实例。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	Task<TResult> HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken = default);
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
	/// <remarks>
	/// 通过 <c>await</c> 等待处理结果，确保处理程序中的异常能够正常传播到调用方。
	/// </remarks>
	async Task<Unit> IHandler<TMessage, Unit>.HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken)
	{
		await HandleAsync(message, context, cancellationToken).ConfigureAwait(false);
		return Unit.Value;
	}
}