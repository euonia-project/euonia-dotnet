using System.Security.Claims;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 收件路径上的消息信封适配器，将消费端现有的 <see cref="MessageContext"/> 与负载包装为 <see cref="IMessageEnvelope"/>。
/// </summary>
/// <remarks>
/// 该类型用于收件箱（Inbox）去重与重试场景：将已接收的消息连同其上下文信息写入收件箱存储，
/// 以便后续重试时能够还原完整的消息信封。
/// </remarks>
internal sealed class InboundEnvelope : IMessageEnvelope
{
	private readonly MessageContext _context;
	private readonly object _payload;

	/// <summary>
	/// 初始化 <see cref="InboundEnvelope"/> 类的新实例。
	/// </summary>
	/// <param name="context">消息上下文。</param>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="payload">消息负载。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="context"/> 或 <paramref name="payload"/> 为 <c>null</c> 时抛出。</exception>
	public InboundEnvelope(MessageContext context, string channel, object payload)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		ArgumentNullException.ThrowIfNull(payload);
		_payload = payload;
		Channel = channel;
	}

	/// <inheritdoc/>
	public string MessageId => _context.MessageId;

	/// <inheritdoc/>
	public string CorrelationId => _context.CorrelationId;

	/// <inheritdoc/>
	public string ConversationId => _context.ConversationId;

	/// <inheritdoc/>
	public string RequestTraceId => _context.RequestTraceId;

	/// <inheritdoc/>
	public string Channel { get; }

	/// <inheritdoc/>
	public string Authorization => _context.Authorization;

	/// <inheritdoc/>
	public long Timestamp => 0;

	/// <inheritdoc/>
	public string TypeName => _payload.GetType().FullName;

	/// <inheritdoc/>
	public MessageMetadata Metadata => _context.Metadata;

	/// <inheritdoc/>
	public ClaimsPrincipal User => _context.User as ClaimsPrincipal;

	/// <inheritdoc/>
	public object Payload => _payload;
}