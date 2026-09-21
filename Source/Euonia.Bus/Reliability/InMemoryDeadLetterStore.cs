using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 基于内存 <see cref="ConcurrentDictionary{TKey, TValue}"/> 的死信（Dead Letter）存储实现，仅用于开发与参考。
/// </summary>
/// <remarks>
/// 数据仅保存在当前进程内，进程重启后丢失。生产环境请使用数据库等持久化实现。
/// </remarks>
public class InMemoryDeadLetterStore : IDeadLetterStore
{
	private readonly ConcurrentDictionary<string, DeadLetterEntry> _entries = new();

	/// <inheritdoc/>
	public bool Add(DeadLetterEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		return _entries.TryAdd(entry.MessageId, entry);
	}

	/// <inheritdoc/>
	public DeadLetterEntry Get(string messageId)
	{
		return _entries.GetValueOrDefault(messageId);
	}

	/// <inheritdoc/>
	public IReadOnlyList<DeadLetterEntry> GetAll()
	{
		return [.. _entries.Values];
	}

	/// <inheritdoc/>
	public bool Remove(string messageId)
	{
		return _entries.TryRemove(messageId, out _);
	}
}
