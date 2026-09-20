using System.Collections.Concurrent;

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
	/// 发件箱条目缓存，避免重复读取已加载的条目。
	/// </summary>
	static readonly ConcurrentDictionary<string, OutboxEntry> Cache = new();

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
	/// 优先从 <see cref="Cache"/> 读取；缓存未命中时调用 <see cref="Get"/> 并将结果缓存。
	/// </remarks>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的发件箱条目；未找到时返回 <c>null</c>。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="messageId"/> 为 <c>null</c> 或空白时抛出。</exception>
	OutboxEntry GetAndCache(string messageId)
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
	/// 清空发件箱条目缓存。
	/// </summary>
	void ClearCache()
	{
		Cache.Clear();
	}

	/// <summary>
	/// 获取所有投递失败且等待重试的传输记录，供后台调度器重试。
	/// </summary>
	/// <returns>失败的传输记录集合。</returns>
	IReadOnlyList<OutboxTransport> GetFailedMessages();
}