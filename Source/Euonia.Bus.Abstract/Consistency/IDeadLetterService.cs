namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 死信（Dead Letter）查询与重放服务。
/// </summary>
/// <remarks>
/// 重放按死信来源分两条路径：
/// <list type="bullet">
/// <item><description>发件箱来源：把信封重新投递到原传输器；</description></item>
/// <item><description>收件箱来源：把消息交给 <see cref="IHandlerContext"/> 在原通道上重新处理。</description></item>
/// </list>
/// 重放成功后该死信记录会被移除；失败时保留记录并把新的错误信息写回 <see cref="DeadLetterEntry.Error"/>。
/// </remarks>
public interface IDeadLetterService
{
	/// <summary>
	/// 获取全部死信记录。
	/// </summary>
	/// <returns>死信记录集合。</returns>
	IReadOnlyList<DeadLetterEntry> GetAll();

	/// <summary>
	/// 根据消息标识符获取死信记录；未找到时返回 <c>null</c>。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>对应的死信记录；未找到时返回 <c>null</c>。</returns>
	DeadLetterEntry Get(string messageId);

	/// <summary>
	/// 重放指定的死信记录。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>重放成功并已移除记录返回 <c>true</c>；记录不存在或重放失败返回 <c>false</c>。</returns>
	Task<bool> ReplayAsync(string messageId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 丢弃指定的死信记录（不做重放，直接移除）。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>移除成功返回 <c>true</c>；记录不存在时返回 <c>false</c>。</returns>
	bool Discard(string messageId);
}
