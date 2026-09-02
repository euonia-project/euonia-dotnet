namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines the message header keys.
/// </summary>
public static class MessageHeaders
{
	/**
     * 会话 ID
     */
	public const string ConversationId = "x-conversation-id";

	/**
	 * 请求追踪 ID
	 */
	public const string RequestTraceId = "x-request-trace-id";

	/**
	 * 授权令牌
	 */
	public const string Authorization = "x-authorization";

	/**
	 * 关联 ID
	 */
	public const string CorrelationId = "x-correlation-id";

	/**
	 * 消息 ID
	 */
	public const string MessageId = "x-message-id";

	/**
	 * 消息类型
	 */
	public const string MessageType = "x-message-type";

	/**
	 * 内容类型
	 */
	public const string ContentType = "x-content-type";

	/**
	 * 内容编码
	 */
	public const string ContentEncoding = "x-content-encoding";

	/**
	 * 投递模式
	 */
	public const string DeliveryMode = "x-delivery-mode";

	/**
	 * 时间戳
	 */
	public const string Timestamp = "x-timestamp";

	/**
	 * 优先级
	 */
	public const string Priority = "x-priority";

	/**
	 * 过期时间
	 */
	public const string Expiration = "x-expiration";

	/**
	 * 回复地址
	 */
	public const string ReplyTo = "x-reply-to";

	/**
	 * 类型
	 */
	public const string Type = "x-type";

	/**
	 * 用户 ID
	 */
	public const string UserId = "x-user-id";

	/**
	 * 路由键
	 */
	public const string RoutingKey = "x-routing-key";
	
	/// <summary>
	/// 消息通道
	/// </summary>
	public const string Channel = "x-channel";
}