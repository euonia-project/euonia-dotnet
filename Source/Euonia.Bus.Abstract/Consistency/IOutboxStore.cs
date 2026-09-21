namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 发件箱（Outbox）持久化存储接口。
/// </summary>
/// <remarks>
/// 调用方（消息发送端）在事务中将待发送消息插入发件箱存储，
/// 后台调度器读取失败记录并重新投递，投递成功或失败后更新对应传输记录。
/// 实现应提供持久化能力；内存实现仅用于开发与参考。</remarks>
public interface IOutboxStore
{
	/// <summary>
	/// 将一条消息及其涉及的传输器列表插入发件箱存储。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <param name="transports">该消息需要投递的传输器名称列表。</param>
	/// <returns>插入成功返回 <c>true</c>；当消息已存在（重复）时返回 <c>false</c>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="message"/> 或 <paramref name="transports"/> 为 <c>null</c> 时抛出。</exception>
	bool Insert(IMessageEnvelope message, IReadOnlyList<string> transports)
	{
		ArgumentNullException.ThrowIfNull(message);
		ArgumentNullException.ThrowIfNull(transports);

		var entry = new OutboxEntry
		{
			MessageId = message.MessageId,
			Channel = message.Channel,
			MessageType = message.Payload?.GetType().FullName,
			Content = message,
			CreatedAt = DateTime.Now,
		};

		foreach (var transportName in transports)
		{
			entry.AddTransport(transportName);
		}

		return Insert(entry);
	}

	/// <summary>
	/// 将指定的发件箱条目插入存储。
	/// </summary>
	/// <param name="entry">要插入的发件箱条目。</param>
	/// <returns>插入成功返回 <c>true</c>；当消息已存在（重复）时返回 <c>false</c>。</returns>
	bool Insert(OutboxEntry entry);

	/// <summary>
	/// 将指定消息在指定传输器上的发送状态标记为成功。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <param name="transport">传输器名称。</param>
	void MarkAsSuccess(string messageId, string transport);

	/// <summary>
	/// 将指定消息在指定传输器上的发送状态标记为失败。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <param name="transport">传输器名称。</param>
	/// <param name="errorMessage">失败原因的描述信息。</param>
	void MarkAsFailed(string messageId, string transport, string errorMessage);

	/// <summary>
	/// 根据消息标识符获取发件箱条目；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的发件箱条目；未找到时返回 <c>null</c>。</returns>
	OutboxEntry Get(string messageId);

	/// <summary>
	/// 获取发件箱条目并写入缓存。
	/// </summary>
	/// <remarks>
	/// 优先从当前存储实例的缓存读取；缓存未命中时调用 <see cref="Get"/> 并将结果缓存。
	/// 缓存按存储实例隔离，不同实例之间互不影响。
	/// </remarks>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的发件箱条目；未找到时返回 <c>null</c>。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="messageId"/> 为 <c>null</c> 或空白时抛出。</exception>
	OutboxEntry GetAndCache(string messageId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
		return StoreEntryCache<IOutboxStore, OutboxEntry>.GetOrAdd(this, messageId, Get);
	}

	/// <summary>
	/// 清空当前存储实例的发件箱条目缓存。
	/// </summary>
	void ClearCache()
	{
		StoreEntryCache<IOutboxStore, OutboxEntry>.Clear(this);
	}

	/// <summary>
	/// 获取所有投递失败且等待重试的传输记录，供后台调度器重试。
	/// </summary>
	/// <returns>失败的传输记录集合。</returns>
	/// <remarks>
	/// 不应返回已转入死信（<see cref="OutboxTransportStatus.DeadLettered"/>）的记录，
	/// 否则它们会被每轮轮询反复扫描。
	/// </remarks>
	IReadOnlyList<OutboxTransport> GetFailedMessages();

	/// <summary>
	/// 清理早于 <paramref name="cutoff"/> 且已终结（全部传输记录均为
	/// <see cref="OutboxTransportStatus.Success"/> 或 <see cref="OutboxTransportStatus.DeadLettered"/>）的条目。
	/// </summary>
	/// <remarks>
	/// 默认实现为空操作，因此既有实现无需改动即可继续工作。
	/// 持久化实现应真正删除记录，避免存储无界增长。
	/// <paramref name="cutoff"/> 与 <see cref="OutboxEntry.CreatedAt"/> 均为本地时间。
	/// </remarks>
	/// <param name="cutoff">清理截止时间（本地时间）；早于该时间的已终结条目将被移除。</param>
	void Cleanup(DateTime cutoff)
	{
	}
}