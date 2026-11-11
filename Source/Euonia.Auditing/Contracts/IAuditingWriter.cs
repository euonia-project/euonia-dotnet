namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义审计记录写入队列契约；写入失败不得影响业务。
/// </summary>
public interface IAuditingWriter
{
	/// <summary>
	/// 入队一条审计记录。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示入队操作的任务。</returns>
	ValueTask WriteAsync(AuditingRecord record, CancellationToken cancellationToken = default);

	/// <summary>
	/// 等待当前队列排空。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示排空操作的任务。</returns>
	Task FlushAsync(CancellationToken cancellationToken = default);
}
