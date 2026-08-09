namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 基于 Lambda 表达式的消息处理程序，支持返回响应结果。
/// </summary>
/// <typeparam name="TMessage">消息类型。</typeparam>
/// <typeparam name="TResult">响应类型。</typeparam>
public class LambdaHandler<TMessage, TResult>
{
	private readonly Func<TMessage, IMessageContext, Task<TResult>> _handler;

	/// <summary>
	/// 初始化 <see cref="LambdaHandler{T, R}"/> 类的新实例。
	/// </summary>
	/// <param name="handler">用于处理消息的异步委托。</param>
	public LambdaHandler(Func<TMessage, IMessageContext, Task<TResult>> handler)
	{
		_handler = handler;
	}

	/// <summary>
	/// 处理消息并返回响应结果。
	/// </summary>
	/// <param name="message">消息实例。</param>
	/// <param name="messageContext">消息上下文。</param>
	/// <returns>包含响应结果的异步任务。</returns>
	public Task<TResult> HandleAsync(TMessage message, IMessageContext messageContext)
	{
		return _handler.Invoke(message, messageContext);
	}
}

/// <summary>
/// 基于 Lambda 表达式的消息处理程序，无返回值。
/// </summary>
/// <typeparam name="TMessage">消息类型。</typeparam>
public class LambdaHandler<TMessage>
{
	private readonly Func<TMessage, IMessageContext, Task> _handler;

	/// <summary>
	/// 初始化 <see cref="LambdaHandler{T}"/> 类的新实例。
	/// </summary>
	/// <param name="handler">用于处理消息的异步委托。</param>
	public LambdaHandler(Func<TMessage, IMessageContext, Task> handler)
	{
		_handler = handler;
	}

	/// <summary>
	/// 处理消息。
	/// </summary>
	/// <param name="message">消息实例。</param>
	/// <param name="messageContext">消息上下文。</param>
	/// <returns>表示异步操作的任务。</returns>
	public Task HandleAsync(TMessage message, IMessageContext messageContext)
	{
		return _handler.Invoke(message, messageContext);
	}
}