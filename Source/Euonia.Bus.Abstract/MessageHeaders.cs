namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines the message header keys.
/// </summary>
public static class MessageHeaders
{
	/**
     * 会话 ID
     */
	public const string CONVERSATION_ID = "x-conversation-id";

	/**
	 * 请求追踪 ID
	 */
	public const string REQUEST_TRACE_ID = "x-request-trace-id";

	/**
	 * 授权令牌
	 */
	public const string AUTHORIZATION = "x-authorization";

	/**
	 * 关联 ID
	 */
	public const string CORRELATION_ID = "x-correlation-id";

	/**
	 * 消息 ID
	 */
	public const string MESSAGE_ID = "x-message-id";

	/**
	 * 消息类型
	 */
	public const string MESSAGE_TYPE = "x-message-type";

	/**
	 * 内容类型
	 */
	public const string CONTENT_TYPE = "x-content-type";

	/**
	 * 内容编码
	 */
	public const string CONTENT_ENCODING = "x-content-encoding";

	/**
	 * 投递模式
	 */
	public const string DELIVERY_MODE = "x-delivery-mode";

	/**
	 * 时间戳
	 */
	public const string TIMESTAMP = "x-timestamp";

	/**
	 * 优先级
	 */
	public const string PRIORITY = "x-priority";

	/**
	 * 过期时间
	 */
	public const string EXPIRATION = "x-expiration";

	/**
	 * 回复地址
	 */
	public const string REPLY_TO = "x-reply-to";

	/**
	 * 类型
	 */
	public const string TYPE = "x-type";

	/**
	 * 用户 ID
	 */
	public const string USER_ID = "x-user-id";

	/**
	 * 路由键
	 */
	public const string ROUTING_KEY = "x-routing-key";
}