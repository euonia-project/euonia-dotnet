namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息处理期间 <see cref="RequestContext"/> 的来源。
/// </summary>
public enum MessageRequestContextSource
{
	/// <summary>
	/// 仅当当前执行流没有请求上下文时，从消息信封恢复。
	/// HTTP/gRPC 入站请求的上下文因此得以保留。
	/// </summary>
	Envelope = 0,

	/// <summary>
	/// 始终从消息信封恢复；信封不携带身份信息时安装 <c>null</c>。
	/// 用于死信重放、收件箱重执行与跨进程消费，避免沿用触发方或其他消息的身份。
	/// </summary>
	EnvelopeOverride = 1,

	/// <summary>
	/// 不修改当前请求上下文；用于后台出站重投递等与消息处理身份无关的场景。
	/// </summary>
	Inherit = 2,

	/// <summary>
	/// 使用发布时按消息捕获的完整发布方快照；<see cref="RequestContext.RequestServices"/>
	/// 在处理时重绑定到实际执行作用域。
	/// </summary>
	PublisherSnapshot = 3,
}
