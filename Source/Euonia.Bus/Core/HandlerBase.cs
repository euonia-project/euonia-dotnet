namespace Nerosoft.Euonia.Bus;

/// <summary>
/// <see cref="IHandler{TMessage}" /> 的抽象实现。
/// </summary>
/// <typeparam name="TMessage">要处理的消息类型。</typeparam>
/// <seealso cref="IHandler{TMessage}" />
public abstract class HandlerBase<TMessage> : IHandler<TMessage>
	where TMessage : class
{
	/// <summary>
	/// 判断当前实例能否处理指定的消息类型。
	/// </summary>
	/// <param name="messageType">要检查的消息类型。</param>
	/// <returns>如果可以处理指定消息类型，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public virtual bool CanHandle(Type messageType)
	{
		return typeof(TMessage) == messageType;
	}

	/// <inheritdoc />
	Task IHandler<TMessage>.HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken)
	{
		return HandleAsync(message, context, cancellationToken);
	}

	/// <summary>
	/// 异步处理指定的消息。
	/// </summary>
	/// <param name="message">要处理的消息实例。</param>
	/// <param name="messageContext">消息上下文，包含处理状态与结果。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步处理操作的任务，包含处理结果 <see cref="Unit" /> 实例。</returns>
	public abstract Task<Unit> HandleAsync(TMessage message, IMessageContext messageContext, CancellationToken cancellationToken = default);
}