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
		return _entries.TryAdd(entry.Id, entry);
	}

	/// <inheritdoc/>
	public DeadLetterEntry Get(string messageId)
	{
		return _entries.Values.FirstOrDefault(entry => string.Equals(entry.MessageId, messageId, StringComparison.Ordinal));
	}

	/// <inheritdoc/>
	public DeadLetterEntry GetById(string id)
	{
		return _entries.GetValueOrDefault(id);
	}

	/// <inheritdoc/>
	public void Update(DeadLetterEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		_entries[entry.Id] = entry;
	}

	/// <inheritdoc/>
	public IReadOnlyList<DeadLetterEntry> GetAll()
	{
		return [.. _entries.Values];
	}

	/// <inheritdoc/>
	public bool Remove(string messageId)
	{
		var entry = Get(messageId);
		return entry != null && _entries.TryRemove(entry.Id, out _);
	}

	/// <inheritdoc/>
	public bool RemoveById(string id)
	{
		return _entries.TryRemove(id, out _);
	}
}
