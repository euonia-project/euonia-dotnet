using System.Security.Claims;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息信封，封装了消息的元数据、负载及相关上下文信息。
/// </summary>
public interface IMessageEnvelope
{
	/// <summary>
	/// 获取消息的唯一标识符。
	/// </summary>
	string MessageId { get; }

	/// <summary>
	/// 获取关联标识符，用于关联相关的消息。
	/// </summary>
	string CorrelationId { get; }

	/// <summary>
	/// 获取会话标识符，用于标识消息所属的会话。
	/// </summary>
	string ConversationId { get; }

	/// <summary>
	/// 获取请求追踪标识符，用于追踪请求链路。
	/// </summary>
	string RequestTraceId { get; }

	/// <summary>
	/// 获取消息通道名称。
	/// </summary>
	string Channel { get; }

	/// <summary>
	/// 获取授权信息，如令牌或凭证。
	/// </summary>
	string Authorization { get; }

	/// <summary>
	/// 获取消息的时间戳（Unix 毫秒）。
	/// </summary>
	long Timestamp { get; }

	/// <summary>
	/// 获取消息负载的类型名称。
	/// </summary>
	string TypeName { get; }

	/// <summary>
	/// 获取消息的附加元数据。
	/// </summary>
	MessageMetadata Metadata { get; }

	/// <summary>
	/// 用户信息
	/// </summary>
	ClaimsPrincipal User { get; }
	
	/// <summary>
	/// 获取消息负载内容。
	/// </summary>
	object Payload { get; }
}

/// <summary>
/// 表示消息信封，封装了消息的元数据、负载及相关上下文信息。
/// </summary>
/// <typeparam name="T">消息负载的类型。</typeparam>
public interface IMessageEnvelope<out T> : IMessageEnvelope
{
	/// <summary>
	/// 获取消息负载内容。
	/// </summary>
	new T Payload { get; }
}