using System.Collections.Concurrent;

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
	/// 收件箱条目缓存，避免重复读取已加载的条目。
	/// </summary>
	static readonly ConcurrentDictionary<string, InboxEntry> Cache = new();

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
	/// 根据消息标识符获取收件箱条目；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的收件箱条目；未找到时返回 <c>null</c>。</returns>
	InboxEntry Get(string messageId);

	/// <summary>
	/// 获取收件箱条目并写入缓存。
	/// </summary>
	/// <remarks>
	/// 优先从 <see cref="Cache"/> 读取；缓存未命中时调用 <see cref="Get"/> 并将结果缓存。
	/// </remarks>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的收件箱条目；未找到时返回 <c>null</c>。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="messageId"/> 为 <c>null</c> 或空白时抛出。</exception>
	InboxEntry GetAndCache(string messageId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
		if (Cache.TryGetValue(messageId, out var cached))
		{
			return cached;
		}

		var entry = Get(messageId);
		if (entry != null)
		{
			Cache[messageId] = entry;
		}

		return entry;
	}

	/// <summary>
	/// 清空收件箱条目缓存。
	/// </summary>
	void ClearCache()
	{
		Cache.Clear();
	}

	/// <summary>
	/// 获取所有执行失败且等待重试的处理程序记录，供后台调度器重试。
	/// </summary>
	/// <returns>失败的处理程序记录集合。</returns>
	IReadOnlyList<InboxHandler> GetFailedMessages();
}