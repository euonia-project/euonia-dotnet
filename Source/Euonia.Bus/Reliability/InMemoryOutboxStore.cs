using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;
/// <summary>
/// 基于内存 <see cref="ConcurrentDictionary{TKey, TValue}"/> 的发件箱（Outbox）存储实现，仅用于开发与参考。
/// </summary>
/// <remarks>
/// 数据仅保存在当前进程内，进程重启后丢失。生产环境请使用数据库等持久化实现。
/// </remarks>
public class InMemoryOutboxStore : IOutboxStore
{
	private readonly ConcurrentDictionary<string, OutboxEntry> _entries = new();

	/// <inheritdoc/>
	public bool Insert(OutboxEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		return _entries.TryAdd(entry.MessageId, entry);
	}

	/// <inheritdoc/>
	public OutboxEntry Get(string messageId)
	{
		return _entries.GetValueOrDefault(messageId);
	}

	/// <inheritdoc/>
	public void MarkAsSuccess(string messageId, string transport)
	{
		var entry = Get(messageId);
		var item = entry?.GetTransport(transport);
		if (item != null)
		{
			item.MarkAsSuccess();
		}
	}

	/// <inheritdoc/>
	public void MarkAsFailed(string messageId, string transport, string errorMessage)
	{
		var entry = Get(messageId);
		var item = entry?.GetTransport(transport);
		if (item != null)
		{
			item.MarkAsFailed(errorMessage);
		}
	}

	/// <inheritdoc/>
	public IReadOnlyList<OutboxTransport> GetFailedMessages()
	{
		return _entries.Values
		               .SelectMany(entry => entry.Transports)
		               .Where(transport => transport.Status == OutboxTransportStatus.Failed)
		               .ToList();
	}

	/// <inheritdoc/>
	public void Cleanup(DateTime cutoff)
	{
		// 只清理已终结的条目：任何传输记录仍为 Pending / Failed 的条目都要保留，否则会丢失待重试的消息。
		foreach (var entry in _entries.Values)
		{
			if (entry.CreatedAt >= cutoff)
			{
				continue;
			}

			if (entry.Transports.All(transport => transport.Status is OutboxTransportStatus.Success or OutboxTransportStatus.DeadLettered))
			{
				_entries.TryRemove(entry.MessageId, out _);
			}
		}
	}
}