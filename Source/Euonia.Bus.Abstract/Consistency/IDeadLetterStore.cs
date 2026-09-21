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
	/// <exception cref="ArgumentNullException">当 <paramref name="entry"/> 为 <c>null</c> 时抛出。</exception>
	bool Add(DeadLetterEntry entry);

	/// <summary>
	/// 根据消息标识符获取死信记录；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的死信记录；未找到时返回 <c>null</c>。</returns>
	DeadLetterEntry Get(string messageId);

	/// <summary>
	/// 获取全部死信记录。
	/// </summary>
	/// <returns>死信记录集合。</returns>
	IReadOnlyList<DeadLetterEntry> GetAll();

	/// <summary>
	/// 根据消息标识符移除死信记录。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>移除成功返回 <c>true</c>；记录不存在时返回 <c>false</c>。</returns>
	bool Remove(string messageId);
}
