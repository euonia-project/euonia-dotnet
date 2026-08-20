namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息处理程序上下文的协定。
/// </summary>
public interface IHandlerContext
{
	/// <summary>
	/// 当消息被订阅时触发。
	/// </summary>
	event EventHandler<MessageSubscribedEventArgs> MessageSubscribed;

	/// <summary>
	/// 异步处理指定通道中的消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="message">要处理的消息。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default);
}