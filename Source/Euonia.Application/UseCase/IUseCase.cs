namespace Nerosoft.Euonia.Application;

/// <summary>
/// 定义用例的接口。
/// </summary>
public interface IUseCase
{
	/// <summary>
	/// 执行用例。
	/// </summary>
	/// <param name="input">用例输入参数，具体类型由实现约定。</param>
	/// <param name="cancellationToken">用于取消用例执行的令牌。</param>
	/// <returns>用例执行结果，以 <see cref="object"/> 形式返回；无输出的实现通常返回 <see cref="EmptyUseCaseOutput.Instance"/>。</returns>
	Task<object?> ExecuteAsync(object input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义具有输入与输出的用例接口。
/// </summary>
/// <typeparam name="TInput">用例输入参数的类型。</typeparam>
/// <typeparam name="TOutput">用例输出结果的类型。</typeparam>
public interface IUseCase<in TInput, TOutput> : IUseCase
{
	/// <summary>
	/// 执行用例。
	/// </summary>
	/// <param name="input">用例输入参数。</param>
	/// <param name="cancellationToken">用于取消用例执行的令牌。</param>
	/// <returns>用例执行结果。</returns>
	Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	/// <exception cref="InvalidCastException"><paramref name="input"/> 无法转换为 <typeparamref name="TInput"/> 时抛出。</exception>
	async Task<object?> IUseCase.ExecuteAsync(object input, CancellationToken cancellationToken)
		=> await ExecuteAsync((TInput)input, cancellationToken);
}

/// <summary>
/// 定义无输出的用例接口。
/// </summary>
/// <typeparam name="TInput">用例输入参数的类型。</typeparam>
public interface INonOutputUseCase<in TInput> : IUseCase<TInput, EmptyUseCaseOutput>
{
	/// <summary>
	/// 执行用例。
	/// </summary>
	/// <param name="input">用例输入参数。</param>
	/// <param name="cancellationToken">用于取消用例执行的令牌。</param>
	/// <returns>表示执行完成的异步任务，不返回业务输出。</returns>
	new Task ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	async Task<EmptyUseCaseOutput> IUseCase<TInput, EmptyUseCaseOutput>.ExecuteAsync(TInput input, CancellationToken cancellationToken)
	{
		await ExecuteAsync(input, cancellationToken);
		return EmptyUseCaseOutput.Instance;
	}
}

/// <summary>
/// 定义无输入参数的用例接口。
/// </summary>
/// <typeparam name="TOutput">用例输出结果的类型。</typeparam>
public interface INonInputUseCase<TOutput> : IUseCase<EmptyUseCaseInput, TOutput>
{
	/// <summary>
	/// 执行用例。
	/// </summary>
	/// <param name="cancellationToken">用于取消用例执行的令牌。</param>
	/// <returns>用例执行结果。</returns>
	Task<TOutput> ExecuteAsync(CancellationToken cancellationToken = default);

	/// <inheritdoc />
	Task<TOutput> IUseCase<EmptyUseCaseInput, TOutput>.ExecuteAsync(EmptyUseCaseInput _, CancellationToken cancellationToken)
		=> ExecuteAsync(cancellationToken);
}

/// <summary>
/// 定义既无输入参数也无输出的用例接口。
/// </summary>
public interface IParameterlessUseCase : IUseCase<EmptyUseCaseInput, EmptyUseCaseOutput>
{
	/// <summary>
	/// 执行用例。
	/// </summary>
	/// <param name="cancellationToken">用于取消用例执行的令牌。</param>
	/// <returns>表示执行完成的异步任务，不返回业务输出。</returns>
	Task ExecuteAsync(CancellationToken cancellationToken = default);

	/// <inheritdoc />
	async Task<EmptyUseCaseOutput> IUseCase<EmptyUseCaseInput, EmptyUseCaseOutput>.ExecuteAsync(EmptyUseCaseInput _, CancellationToken cancellationToken)
	{
		await ExecuteAsync(cancellationToken);
		return EmptyUseCaseOutput.Instance;
	}
}