namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义审计记录持久化契约。
/// </summary>
/// <remarks>
/// 写入器每批创建 DI 作用域后解析存储：单例或 scoped 均可，scoped 随批释放。
/// 实现不得依赖调用方的请求作用域（后台写入没有请求上下文）。
/// </remarks>
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
