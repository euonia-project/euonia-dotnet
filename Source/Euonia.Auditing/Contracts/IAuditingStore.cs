namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义审计记录持久化契约。
/// </summary>
public interface IAuditingStore
{
	/// <summary>
	/// 保存一条审计记录；同一 <see cref="AuditingRecord.ScopeId"/> 必须幂等。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示保存操作的任务。</returns>
	Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken = default);

	/// <summary>
	/// 批量保存审计记录。
	/// </summary>
	/// <param name="records">审计记录集合。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示保存操作的任务。</returns>
	async Task SaveManyAsync(IReadOnlyCollection<AuditingRecord> records, CancellationToken cancellationToken = default)
	{
		foreach (var record in records)
		{
			await SaveAsync(record, cancellationToken).ConfigureAwait(false);
		}
	}
}
