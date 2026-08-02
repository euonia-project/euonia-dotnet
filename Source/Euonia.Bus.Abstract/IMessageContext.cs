using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息上下文的接口，提供消息处理过程中所需的元数据和身份信息。
/// </summary>
public interface IMessageContext : IDisposable
{
	/// <summary>
	/// 获取或设置消息的唯一标识符。
	/// </summary>
	string MessageId { get; }

	/// <summary>
	/// 获取或设置关联标识符。
	/// </summary>
	string CorrelationId { get; }

	/// <summary>
	/// 获取或设置会话标识符。
	/// </summary>
	string ConversationId { get; }

	/// <summary>
	/// 获取或设置请求追踪标识符。
	/// </summary>
	string RequestTraceId { get; }

	/// <summary>
	/// 获取或设置授权信息。
	/// </summary>
	string Authorization { get; }

	/// <summary>
	/// 获取当前用户。
	/// </summary>
	IPrincipal User { get; }

	/// <summary>
	/// 获取消息请求头。
	/// </summary>
	IReadOnlyDictionary<string, string> Headers { get; }

	/// <summary>
	/// 获取或设置包含消息元数据信息的 <see cref="MessageMetadata"/> 实例。
	/// </summary>
	MessageMetadata Metadata { get; }

	/// <summary>
	/// 向消息分发器回复消息处理结果。
	/// </summary>
	/// <typeparam name="TMessage">消息的类型。</typeparam>
	/// <param name="message">要回复的消息。</param>
	void Response<TMessage>(TMessage message);

	/// <summary>
	/// 在消息处理失败后调用。
	/// </summary>
	/// <param name="exception">处理过程中发生的异常。</param>
	void Failure(Exception exception);
}