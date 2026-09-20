namespace Nerosoft.Euonia.Application;

/// <summary>
/// 定义统一执行用例（Use Case）并分发执行结果到 presenter 的契约接口。
/// </summary>
/// <remarks>
/// 执行器负责串联用例与 <see cref="IUseCasePresenter{TOutput}"/>：
/// 用例成功时调用 <c>presenter.Ok(output)</c>，执行抛出异常时调用 <c>presenter.Error(exception)</c>，
/// 调用方无需自行编写 try/catch 分发逻辑。
/// </remarks>
public interface IUseCaseExecutor
{
	/// <summary>
	/// 执行带输入与输出的用例。
	/// </summary>
	/// <typeparam name="TInput">用例输入类型。</typeparam>
	/// <typeparam name="TOutput">用例输出类型。</typeparam>
	/// <param name="useCase">要执行的用例。</param>
	/// <param name="input">用例输入。</param>
	/// <param name="presenter">接收执行结果的 presenter。</param>
	/// <param name="cancellationToken">用于取消执行操作的令牌。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	Task ExecuteAsync<TInput, TOutput>(IUseCase<TInput, TOutput> useCase, TInput input, IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行只接收输入的用例（无输出）。
	/// </summary>
	/// <typeparam name="TInput">用例输入类型。</typeparam>
	/// <param name="useCase">要执行的用例。</param>
	/// <param name="input">用例输入。</param>
	/// <param name="presenter">接收执行结果的 presenter。</param>
	/// <param name="cancellationToken">用于取消执行操作的令牌。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	Task ExecuteAsync<TInput>(INonOutputUseCase<TInput> useCase, TInput input, IUseCasePresenter<EmptyUseCaseOutput> presenter, CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行只产生输出的用例（无输入）。
	/// </summary>
	/// <typeparam name="TOutput">用例输出类型。</typeparam>
	/// <param name="useCase">要执行的用例。</param>
	/// <param name="presenter">接收执行结果的 presenter。</param>
	/// <param name="cancellationToken">用于取消执行操作的令牌。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	Task ExecuteAsync<TOutput>(INonInputUseCase<TOutput> useCase, IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行无输入且无输出的用例。
	/// </summary>
	/// <param name="useCase">要执行的用例。</param>
	/// <param name="presenter">接收执行结果的 presenter。</param>
	/// <param name="cancellationToken">用于取消执行操作的令牌。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	Task ExecuteAsync(IParameterlessUseCase useCase, IUseCasePresenter<EmptyUseCaseOutput> presenter, CancellationToken cancellationToken = default);
}