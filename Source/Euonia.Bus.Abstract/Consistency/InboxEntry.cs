namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 收件箱（Inbox）中的一条已接收消息记录，包含消息内容及每个处理程序的执行状态。
/// </summary>
/// <remarks>
/// 收件箱模式的核心思路是：在消费端对接收到的消息进行去重与状态追踪，
/// 避免因网络重试等原因导致同一消息被重复处理（至少一次语义）。
/// </remarks>
public class InboxEntry
{
	private readonly List<InboxHandler> _handlers = [];

	/// <summary>
	/// 获取或设置消息的标识符。
	/// </summary>
	public string MessageId { get; set; }

	/// <summary>
	/// 获取或设置消息的通道名称。
	/// </summary>
	public string Channel { get; set; }

	/// <summary>
	/// 获取或设置消息负载的完整类型名称。
	/// </summary>
	public string MessageType { get; set; }

	/// <summary>
	/// 获取或设置消息内容（信封）。
	/// </summary>
	public IMessageEnvelope Content { get; set; }

	/// <summary>
	/// 获取或设置条目的创建时间。
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.Now;

	/// <summary>
	/// 获取已为消息添加的处理程序记录集合。
	/// </summary>
	public IReadOnlyList<InboxHandler> Handlers => _handlers;

	/// <summary>
	/// 为当前消息添加一个处理程序记录。
	/// </summary>
	/// <param name="handler">要添加的处理程序记录。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="handler"/> 为 <c>null</c> 时抛出。</exception>
	public void AddHandler(InboxHandler handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		handler.MessageId = MessageId;
		_handlers.Add(handler);
	}

	/// <summary>
	/// 按处理程序类型名称添加一条待执行的处理记录。
	/// </summary>
	/// <param name="handlerName">处理程序的类型名称。</param>
	/// <exception cref="ArgumentException">当 <paramref name="handlerName"/> 为 <c>null</c> 或空白时抛出。</exception>
	public void AddHandler(string handlerName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);
		_handlers.Add(new InboxHandler
		{
			MessageId = MessageId,
			Name = handlerName,
			Status = InboxHandlerStatus.Pending,
		});
	}

	/// <summary>
	/// 根据处理程序类型名称获取对应的处理记录；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="name">处理程序的类型名称。</param>
	/// <returns>对应的处理记录；未找到时返回 <c>null</c>。</returns>
	public InboxHandler GetHandler(string name)
	{
		return _handlers.FirstOrDefault(h => h.Name == name);
	}

	/// <summary>
	/// 返回当前条目的字符串表示形式。
	/// </summary>
	/// <returns>包含消息标识符、通道与消息类型的字符串。</returns>
	public override string ToString()
	{
		return $"InboxEntry{{messageId={MessageId}, channel={Channel}, messageType={MessageType}}}";
	}
}