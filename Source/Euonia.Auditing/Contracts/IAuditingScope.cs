namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 一次执行作用域的审计句柄。
/// </summary>
public interface IAuditingScope : IAsyncDisposable
{
	/// <summary>
	/// 获取作用域标识。
	/// </summary>
	string ScopeId { get; }

	/// <summary>
	/// 获取作用域记录；完成前可回填租户等字段。
	/// </summary>
	AuditingRecord Record { get; }

	/// <summary>
	/// 获取一个值，指示作用域是否已完成；已完成的作用域不应再被复用。
	/// </summary>
	bool IsCompleted => false;

	/// <summary>
	/// 开始一个动作。
	/// </summary>
	/// <param name="serviceName">服务名称。</param>
	/// <param name="methodName">方法名称。</param>
	/// <param name="actionName">动作显示名称。</param>
	/// <returns>动作句柄。</returns>
	IAuditingAction BeginAction(string serviceName, string methodName, string? actionName = null);

	/// <summary>
	/// 完成作用域并提交记录；幂等。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示提交操作的任务。</returns>
	ValueTask CompleteAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 以失败完成作用域；幂等。
	/// </summary>
	/// <param name="exception">导致失败的异常。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示提交操作的任务。</returns>
	ValueTask AbortAsync(Exception exception, CancellationToken cancellationToken = default);
}
