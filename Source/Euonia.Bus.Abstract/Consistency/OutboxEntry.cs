namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 发件箱（Outbox）中的一条消息记录，包含消息内容及每个传输通道的发送状态。
/// </summary>
/// <remarks>
/// 发件箱模式的核心思路是：在业务事务中原子性地写入发件箱表，
/// 由后台调度器将消息可靠地投递给各传输通道，并在投递成功后标记完成，从而避免消息丢失。
/// </remarks>
public class OutboxEntry
{
	private readonly List<OutboxTransport> _transports = [];

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
	/// 获取已为消息添加的传输记录集合。
	/// </summary>
	public IReadOnlyList<OutboxTransport> Transports => _transports;

	/// <summary>
	/// 为当前消息添加一个传输记录。
	/// </summary>
	/// <param name="transport">要添加的传输记录。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="transport"/> 为 <c>null</c> 时抛出。</exception>
	public void AddTransport(OutboxTransport transport)
	{
		ArgumentNullException.ThrowIfNull(transport);
		transport.MessageId = MessageId;
		_transports.Add(transport);
	}

	/// <summary>
	/// 按传输器名称添加一条待发送的传输记录。
	/// </summary>
	/// <param name="transportName">传输器名称。</param>
	/// <exception cref="ArgumentException">当 <paramref name="transportName"/> 为 <c>null</c> 或空白时抛出。</exception>
	public void AddTransport(string transportName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(transportName);
		_transports.Add(new OutboxTransport
		{
			MessageId = MessageId,
			Name = transportName,
			Status = OutboxTransportStatus.Pending,
		});
	}

	/// <summary>
	/// 根据传输器名称获取对应的传输记录；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="name">传输器名称。</param>
	/// <returns>对应的传输记录；未找到时返回 <c>null</c>。</returns>
	public OutboxTransport GetTransport(string name)
	{
		return _transports.FirstOrDefault(t => t.Name == name);
	}

	/// <summary>
	/// 返回当前条目的字符串表示形式。
	/// </summary>
	/// <returns>包含消息标识符、通道与消息类型的字符串。</returns>
	public override string ToString()
	{
		return $"OutboxEntry{{messageId={MessageId}, channel={Channel}, messageType={MessageType}}}";
	}
}