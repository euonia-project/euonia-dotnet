namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 收件箱（Inbox）持久化存储接口。
/// </summary>
/// <remarks>
/// 调用方（消息消费端）在执行业务处理前将已接收消息插入收件箱存储实现去重，
/// 处理完成或失败后更新对应处理程序记录。实现应提供持久化能力；内存实现仅用于开发与参考。
/// </remarks>
public interface IInboxStore
{
	/// <summary>
	/// 将一条已接收消息及其涉及的处理程序列表插入收件箱存储。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="message">消息信封。</param>
	/// <param name="handlers">该消息需要执行的处理程序类型名称列表。</param>
	/// <returns>插入成功返回 <c>true</c>；当消息已存在（重复）时返回 <c>false</c>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="message"/> 或 <paramref name="handlers"/> 为 <c>null</c> 时抛出。</exception>
	bool Insert(string channel, IMessageEnvelope message, IReadOnlyList<string> handlers)
	{
		ArgumentNullException.ThrowIfNull(message);
		ArgumentNullException.ThrowIfNull(handlers);

		var entry = new InboxEntry
		{
			MessageId = message.MessageId,
			Channel = channel,
			MessageType = message.Payload?.GetType().FullName,
			Content = message,
			CreatedAt = DateTime.Now,
		};

		foreach (var handlerName in handlers)
		{
			entry.AddHandler(handlerName);
		}

		return Insert(entry);
	}

	/// <summary>
	/// 将指定的收件箱条目插入存储。
	/// </summary>
	/// <param name="entry">要插入的收件箱条目。</param>
	/// <returns>插入成功返回 <c>true</c>；当消息已存在（重复）时返回 <c>false</c>。</returns>
	bool Insert(InboxEntry entry);

	/// <summary>
	/// 将指定消息在指定处理程序上的执行状态标记为成功。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <param name="handler">处理程序类型名称。</param>
	void MarkAsSuccess(string messageId, string handler);

	/// <summary>
	/// 将指定消息在指定处理程序上的执行状态标记为失败。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <param name="handler">处理程序类型名称。</param>
	/// <param name="errorMessage">失败原因的描述信息。</param>
	void MarkAsFailed(string messageId, string handler, string errorMessage);

	/// <summary>
	/// 将指定消息在指定处理程序上的执行状态标记为已转入死信队列。
	/// </summary>
	/// <remarks>
	/// 这是一个**终态**：标记后该记录不应再被 <see cref="GetFailedMessages"/> 返回，
	/// 否则后台调度器会每轮重复扫描一条永远不会成功的记录。
	/// <para>
	/// 必须通过本方法持久化终态，而不是去修改 <see cref="GetFailedMessages"/> 返回的对象：
	/// 那只是存储的快照，对它的修改在持久化实现（数据库 / 远程存储）中会丢失。
	/// </para>
	/// </remarks>
	/// <param name="messageId">消息标识符。</param>
	/// <param name="handler">处理程序类型名称。</param>
	/// <param name="errorMessage">导致转入死信的最后一次错误信息。</param>
	void MarkAsDeadLettered(string messageId, string handler, string errorMessage);

	/// <summary>
	/// 根据消息标识符获取收件箱条目；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的收件箱条目；未找到时返回 <c>null</c>。</returns>
	InboxEntry Get(string messageId);

	/// <summary>
	/// 获取收件箱条目并写入缓存。
	/// </summary>
	/// <remarks>
	/// 优先从当前存储实例的缓存读取；缓存未命中时调用 <see cref="Get"/> 并将结果缓存。
	/// 缓存按存储实例隔离，不同实例之间互不影响。
	/// </remarks>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的收件箱条目；未找到时返回 <c>null</c>。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="messageId"/> 为 <c>null</c> 或空白时抛出。</exception>
	InboxEntry GetAndCache(string messageId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
		return StoreEntryCache<IInboxStore, InboxEntry>.GetOrAdd(this, messageId, Get);
	}

	/// <summary>
	/// 清空当前存储实例的收件箱条目缓存。
	/// </summary>
	void ClearCache()
	{
		StoreEntryCache<IInboxStore, InboxEntry>.Clear(this);
	}

	/// <summary>
	/// 获取所有执行失败且等待重试的处理程序记录，供后台调度器重试。
	/// </summary>
	/// <returns>失败的处理程序记录集合。</returns>
	/// <remarks>
	/// 不应返回已转入死信（<see cref="InboxHandlerStatus.DeadLettered"/>）的记录，
	/// 否则它们会被每轮轮询反复扫描。
	/// </remarks>
	IReadOnlyList<InboxHandler> GetFailedMessages();

	/// <summary>
	/// 清理早于 <paramref name="cutoff"/> 且已终结（全部处理程序记录均为
	/// <see cref="InboxHandlerStatus.Success"/> 或 <see cref="InboxHandlerStatus.DeadLettered"/>）的条目。
	/// </summary>
	/// <remarks>
	/// 默认实现为空操作，因此既有实现无需改动即可继续工作。
	/// 持久化实现应真正删除记录，避免存储无界增长。
	/// <paramref name="cutoff"/> 与 <see cref="InboxEntry.CreatedAt"/> 均为本地时间。
	/// </remarks>
	/// <param name="cutoff">清理截止时间（本地时间）；早于该时间的已终结条目将被移除。</param>
	void Cleanup(DateTime cutoff)
	{
	}
}