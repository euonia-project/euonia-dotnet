namespace Nerosoft.Euonia.Application;

/// <summary>
/// <see cref="IUseCaseExecutor"/> 的默认实现。
/// </summary>
/// <remarks>
/// 统一封装了「执行用例 + 结果分发」的过程：
/// <list type="bullet">
/// <item><description>用例正常返回时调用 <c>presenter.Ok(output)</c>；</description></item>
/// <item><description>用例抛出异常时调用 <c>presenter.Error(exception)</c>（含 <see cref="OperationCanceledException"/>，由 presenter 自行决定取消语义）；</description></item>
/// <item><description>无输出/无输入/无参用例通过 <see cref="EmptyUseCaseOutput.Instance"/> 与 <see cref="EmptyUseCaseInput"/> 适配到统一执行路径。</description></item>
/// </list>
/// </remarks>
public class UseCaseExecutor : IUseCaseExecutor
{
	/// <inheritdoc />
	public Task ExecuteAsync<TInput, TOutput>(IUseCase<TInput, TOutput> useCase, TInput input, IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default)
		=> ExecuteCoreAsync(() => useCase.ExecuteAsync(input, cancellationToken), presenter);

	/// <inheritdoc />
	public Task ExecuteAsync<TInput>(INonOutputUseCase<TInput> useCase, TInput input, IUseCasePresenter<EmptyUseCaseOutput> presenter, CancellationToken cancellationToken = default)
		=> ExecuteCoreAsync(async () =>
		{
			await useCase.ExecuteAsync(input, cancellationToken);
			return EmptyUseCaseOutput.Instance;
		}, presenter);

	/// <inheritdoc />
	public Task ExecuteAsync<TOutput>(INonInputUseCase<TOutput> useCase, IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default)
		=> ExecuteCoreAsync(() => useCase.ExecuteAsync(cancellationToken), presenter);

	/// <inheritdoc />
	public Task ExecuteAsync(IParameterlessUseCase useCase, IUseCasePresenter<EmptyUseCaseOutput> presenter, CancellationToken cancellationToken = default)
		=> ExecuteCoreAsync(async () =>
		{
			await useCase.ExecuteAsync(cancellationToken);
			return EmptyUseCaseOutput.Instance;
		}, presenter);

	private static async Task ExecuteCoreAsync<TOutput>(Func<Task<TOutput>> executor, IUseCasePresenter<TOutput> presenter)
	{
		try
		{
			var output = await executor();
			presenter.Ok(output);
		}
		catch (OperationCanceledException exception)
		{
			presenter.Error(exception);
		}
		catch (Exception exception)
		{
			presenter.Error(exception);
		}
	}
}