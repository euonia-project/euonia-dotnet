namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 定义审计记录器：安装作用域、提交记录、刷新队列。
/// </summary>
public interface IAuditingRecorder
{
	/// <summary>
	/// 获取一个值，指示审计是否启用。
	/// </summary>
	bool IsEnabled { get; }

	/// <summary>
	/// 获取当前异步流的审计作用域；不存在时为 <c>null</c>。
	/// </summary>
	IAuditingScope? CurrentScope { get; }

	/// <summary>
	/// 幂等安装作用域：已存在时直接返回当前作用域。
	/// </summary>
	/// <param name="options">作用域创建选项；为 <c>null</c> 时使用隐式作用域。</param>
	/// <returns>审计作用域；审计关闭时为不执行任何操作的空实现。</returns>
	IAuditingScope EnsureScope(AuditingScopeOptions? options = null);

	/// <summary>
	/// 立即提交一条记录（绕过作用域），用于解析失败等场景。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示提交操作的任务。</returns>
	ValueTask SubmitAsync(AuditingRecord record, CancellationToken cancellationToken = default);

	/// <summary>
	/// 等待写入队列排空。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示排空操作的任务。</returns>
	Task FlushAsync(CancellationToken cancellationToken = default);
}
