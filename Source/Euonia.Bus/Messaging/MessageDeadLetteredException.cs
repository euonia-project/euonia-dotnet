using System.Net;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息按策略转入死信；供尚未支持 <see cref="MessageExecutionResult"/> 的旧接收器把裁决转换为失败语义。
/// </summary>
[HttpStatusCode(HttpStatusCode.Accepted)]
public sealed class MessageDeadLetteredException : BusinessException
{
	/// <summary>
	/// 使用消息标识与原因初始化 <see cref="MessageDeadLetteredException"/> 的新实例。
	/// </summary>
	/// <param name="messageId">消息标识。</param>
	/// <param name="reason">转入死信的原因。</param>
	public MessageDeadLetteredException(string? messageId, string reason)
		: base("MessageDeadLettered", $"Message '{messageId}' was dead-lettered: {reason}")
	{
	}
}
