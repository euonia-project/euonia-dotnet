using System.Collections.Concurrent;
using Nerosoft.Euonia.Bus.Tests.Events;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

/// <summary>
/// 示例事件订阅者，记录收到的事件以便断言事件是否送达。
/// </summary>
public class UserEventListener
{
	/// <summary>
	/// 获取已接收事件的多播集合，供测试断言使用。
	/// </summary>
	public static readonly ConcurrentBag<UserCreatedEvent> Received = new();

	/// <summary>
	/// 接收 <see cref="UserCreatedEvent"/> 并加入 <see cref="Received"/> 集合。
	/// </summary>
	/// <param name="message">收到的事件。</param>
	/// <param name="messageContext">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示处理完成的任务。</returns>
	[Subscribe("user.created")]
	public Task HandleAsync(UserCreatedEvent message, IMessageContext messageContext, CancellationToken cancellationToken = default)
	{
		Received.Add(message);
		return Task.CompletedTask;
	}
}