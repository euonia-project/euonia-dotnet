namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 死信（Dead Letter）持久化存储接口。
/// </summary>
/// <remarks>
/// 后台调度器在重试次数耗尽时把消息写入本存储，使其不再参与轮询；
/// 运维人员再通过 <see cref="IDeadLetterService"/> 查询并决定重放或丢弃。
/// 实现应提供持久化能力；内存实现仅用于开发与参考。
/// </remarks>
public interface IDeadLetterStore
{
	/// <summary>
	/// 添加一条死信记录。
	/// </summary>
	/// <param name="entry">要添加的死信记录。</param>
	/// <returns>添加成功返回 <c>true</c>；当同标识符记录已存在时返回 <c>false</c>。</returns>
	/// <remarks>
	/// 实现应以 <see cref="DeadLetterEntry.Id"/>（消息 + 来源 + 目标）作为唯一键，
	/// 同一条消息在多个传输器或多个处理器上的失败应分别保留。
	/// </remarks>
	/// <exception cref="ArgumentNullException">当 <paramref name="entry"/> 为 <c>null</c> 时抛出。</exception>
	bool Add(DeadLetterEntry entry);

	/// <summary>
	/// 更新一条已存在的死信记录（例如重放失败后的错误信息）。
	/// </summary>
	/// <param name="entry">要更新的死信记录。</param>
	/// <remarks>默认实现为空操作，持久化实现应真正落库。</remarks>
	void Update(DeadLetterEntry entry)
	{
	}

	/// <summary>
	/// 根据消息标识符获取第一条死信记录；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的死信记录；未找到时返回 <c>null</c>。</returns>
	DeadLetterEntry Get(string messageId);

	/// <summary>
	/// 根据死信记录唯一标识获取记录；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="id">死信记录唯一标识（<see cref="DeadLetterEntry.Id"/>）。</param>
	/// <returns>对应的死信记录；未找到时返回 <c>null</c>。</returns>
	DeadLetterEntry GetById(string id)
	{
		return GetAll().FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.Ordinal));
	}

	/// <summary>
	/// 获取全部死信记录。
	/// </summary>
	/// <returns>死信记录集合。</returns>
	IReadOnlyList<DeadLetterEntry> GetAll();

	/// <summary>
	/// 根据消息标识符移除第一条死信记录。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>移除成功返回 <c>true</c>；记录不存在时返回 <c>false</c>。</returns>
	bool Remove(string messageId);

	/// <summary>
	/// 根据死信记录唯一标识移除记录。
	/// </summary>
	/// <param name="id">死信记录唯一标识（<see cref="DeadLetterEntry.Id"/>）。</param>
	/// <returns>移除成功返回 <c>true</c>；记录不存在时返回 <c>false</c>。</returns>
	/// <remarks>默认实现按消息 ID 回退删除；存在多个目标时，实现方应覆盖以精确删除指定记录。</remarks>
	bool RemoveById(string id)
	{
		var entry = GetById(id);
		return entry != null && Remove(entry.MessageId);
	}
}
