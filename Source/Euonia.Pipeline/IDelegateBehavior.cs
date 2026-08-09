namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 用于环绕内部处理程序的委托行为（Delegate behavior）。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
public interface IDelegateBehavior<in TRequest>
{
	/// <summary>
	/// 处理请求。
	/// </summary>
	/// <param name="request">要处理的请求实例。</param>
	/// <param name="next">用于调用管道中下一个处理阶段的委托。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步处理操作的任务。</returns>
	Task HandleAsync(TRequest request, Delegate next, CancellationToken cancellationToken);
}