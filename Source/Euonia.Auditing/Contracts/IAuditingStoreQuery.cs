namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义审计记录查询与保留清理契约；由具体存储按需实现。
/// </summary>
public interface IAuditingStoreQuery
{
	/// <summary>
	/// 按条件查询审计记录（按执行时间倒序、含动作）。
	/// </summary>
	/// <param name="query">查询条件。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>审计记录列表。</returns>
	Task<IReadOnlyList<AuditingRecord>> QueryAsync(AuditingQuery query, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按作用域标识查找单条记录。
	/// </summary>
	/// <param name="scopeId">作用域标识。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>审计记录；不存在时为 <c>null</c>。</returns>
	Task<AuditingRecord?> FindByScopeAsync(string scopeId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除早于指定时间的记录。
	/// </summary>
	/// <param name="cutoff">截止时间（UTC，不含）。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>删除的记录数。</returns>
	Task<int> DeleteBeforeAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}
