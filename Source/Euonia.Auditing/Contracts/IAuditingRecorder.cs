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
	/// 幂等安装作用域；<paramref name="force"/> 为 <c>true</c> 时即使全局关闭也创建作用域（用于 <c>[Audited]</c>）。
	/// </summary>
	/// <param name="options">作用域创建选项。</param>
	/// <param name="force">是否忽略全局开关。</param>
	/// <returns>审计作用域。</returns>
	IAuditingScope EnsureScope(AuditingScopeOptions? options, bool force);

	/// <summary>
	/// 创建一个新作用域并设为当前作用域；忽略已存在的环境作用域。
	/// </summary>
	/// <param name="options">作用域创建选项；为 <c>null</c> 时使用隐式作用域。</param>
	/// <returns>新作用域；审计关闭时为不执行任何操作的空实现。</returns>
	IAuditingScope CreateScope(AuditingScopeOptions? options = null);

	/// <summary>
	/// 创建新作用域但不完成/提交旧作用域；调用方负责保存并恢复旧作用域。
	/// </summary>
	/// <param name="options">作用域创建选项。</param>
	/// <returns>新作用域。</returns>
	IAuditingScope CreateDetachedScope(AuditingScopeOptions? options = null);

	/// <summary>
	/// 恢复此前的作用域；仅当当前没有作用域时生效。
	/// </summary>
	/// <param name="scope">要恢复的作用域；为 <c>null</c> 时忽略。</param>
	void Attach(IAuditingScope? scope);

	/// <summary>
	/// 从当前异步流分离作用域；仅当作用域仍是当前作用域时生效。
	/// </summary>
	/// <param name="scope">要分离的作用域。</param>
	void Detach(IAuditingScope scope);

	/// <summary>
	/// 在后台完成作用域并提交记录，不阻塞调用方；异常只记录日志。
	/// </summary>
	/// <param name="scope">要完成的作用域。</param>
	void CompleteInBackground(IAuditingScope scope);

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
