using Microsoft.Extensions.DependencyInjection;

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
/// <item><description>容器解析重载从 <see cref="IServiceProvider"/> 解析用例实例后走同一执行路径。</description></item>
/// </list>
/// </remarks>
public class UseCaseExecutor : IUseCaseExecutor
{
	private readonly IServiceScopeFactory _scopeFactory;

	/// <summary>
	/// 初始化 <see cref="UseCaseExecutor"/> 类的新实例。
	/// </summary>
	/// <param name="serviceProvider">用于解析用例实例的服务容器。</param>
	public UseCaseExecutor(IServiceProvider serviceProvider)
	{
		_scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
	}

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

	/// <inheritdoc />
	public Task ExecuteAsync<TUseCase, TInput, TOutput>(TInput input, IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default)
		where TUseCase : class, IUseCase<TInput, TOutput>
		=> ExecuteCoreAsync(() => GetRequiredService<TUseCase>().ExecuteAsync(input, cancellationToken), presenter);

	/// <inheritdoc />
	public Task ExecuteAsync<TUseCase, TInput>(TInput input, IUseCasePresenter<EmptyUseCaseOutput> presenter, CancellationToken cancellationToken = default)
		where TUseCase : class, INonOutputUseCase<TInput>
		=> ExecuteCoreAsync(async () =>
		{
			await GetRequiredService<TUseCase>().ExecuteAsync(input, cancellationToken);
			return EmptyUseCaseOutput.Instance;
		}, presenter);

	/// <inheritdoc />
	public Task ExecuteAsync<TUseCase, TOutput>(IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default)
		where TUseCase : class, INonInputUseCase<TOutput>
		=> ExecuteCoreAsync(() => GetRequiredService<TUseCase>().ExecuteAsync(cancellationToken), presenter);

	/// <inheritdoc />
	public Task ExecuteAsync<TUseCase>(IUseCasePresenter<EmptyUseCaseOutput> presenter, CancellationToken cancellationToken = default)
		where TUseCase : class, IParameterlessUseCase
		=> ExecuteCoreAsync(async () =>
		{
			await GetRequiredService<TUseCase>().ExecuteAsync(cancellationToken);
			return EmptyUseCaseOutput.Instance;
		}, presenter);

	private TUseCase GetRequiredService<TUseCase>()
		where TUseCase : class
	{
		using var scope = _scopeFactory.CreateScope();
		return scope.ServiceProvider.GetRequiredService<TUseCase>();
	}

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