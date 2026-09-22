namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 表示工作单元的执行上下文，提供保存变更以及控制完成（提交或回滚）的操作。
/// </summary>
/// <remarks>
/// 实现类应在释放时释放其持有的资源。
/// </remarks>
public interface IUnitOfWorkContext : IDisposable
{
	/// <summary>
	/// 保存当前工作单元内的所有挂起变更。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌，默认为 <see cref="CancellationToken.None"/>。</param>
	/// <returns>表示异步保存操作的任务。</returns>
	/// <remarks>
	/// 该操作不一定以事务方式完成工作单元；若需最终提交，请调用 <see cref="CommitAsync(CancellationToken)"/>。
	/// </remarks>
	Task SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 提交工作单元，使所有操作永久生效（例如提交数据库事务）。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌，默认为 <see cref="CancellationToken.None"/>。</param>
	/// <returns>表示异步提交操作的任务。</returns>
	/// <remarks>提交成功后，工作单元即被视为已完成。</remarks>
	Task CommitAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 回滚工作单元，撤销所有未完成的操作（例如回滚数据库事务）。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌，默认为 <see cref="CancellationToken.None"/>。</param>
	/// <returns>表示异步回滚操作的任务。</returns>
	/// <remarks>实现类应确保回滚后系统仍处于一致状态。</remarks>
	Task RollbackAsync(CancellationToken cancellationToken = default);
}