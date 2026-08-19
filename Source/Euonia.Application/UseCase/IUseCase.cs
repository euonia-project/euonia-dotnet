namespace Nerosoft.Euonia.Application;

/// <summary>
/// Defines an interface for use case.
/// </summary>
public interface IUseCase
{
	/// <summary>
	/// Executes the use case.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<object> ExecuteAsync(object input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines an interface for use case.
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TOutput"></typeparam>
public interface IUseCase<in TInput, TOutput> : IUseCase
{
	/// <summary>
	/// Executes the use case.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	async Task<object> IUseCase.ExecuteAsync(object input, CancellationToken cancellationToken)
		=> await ExecuteAsync((TInput)input, cancellationToken);
}

/// <summary>
/// Defines an interface for use case that has non output.
/// </summary>
/// <typeparam name="TInput"></typeparam>
public interface INonOutputUseCase<in TInput> : IUseCase<TInput, EmptyUseCaseOutput>
{
	/// <summary>
	/// Executes the use case.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	new Task ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	async Task<EmptyUseCaseOutput> IUseCase<TInput, EmptyUseCaseOutput>.ExecuteAsync(TInput input, CancellationToken cancellationToken)
	{
		await ExecuteAsync(input, cancellationToken);
		return EmptyUseCaseOutput.Instance;
	}
}

/// <summary>
/// Defines an interface for use case that has non input parameter.
/// </summary>
/// <typeparam name="TOutput"></typeparam>
public interface INonInputUseCase<TOutput> : IUseCase<EmptyUseCaseInput, TOutput>
{
	/// <summary>
	/// Executes the use case.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<TOutput> ExecuteAsync(CancellationToken cancellationToken = default);

	/// <inheritdoc />
	Task<TOutput> IUseCase<EmptyUseCaseInput, TOutput>.ExecuteAsync(EmptyUseCaseInput _, CancellationToken cancellationToken)
		=> ExecuteAsync(cancellationToken);
}

/// <summary>
/// Defines an interface for use case that has non input parameter and non output.
/// </summary>
public interface IParameterlessUseCase : IUseCase<EmptyUseCaseInput, EmptyUseCaseOutput>
{
	/// <summary>
	/// Executes the use case.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task ExecuteAsync(CancellationToken cancellationToken = default);

	/// <inheritdoc />
	async Task<EmptyUseCaseOutput> IUseCase<EmptyUseCaseInput, EmptyUseCaseOutput>.ExecuteAsync(EmptyUseCaseInput _, CancellationToken cancellationToken)
	{
		await ExecuteAsync(cancellationToken);
		return EmptyUseCaseOutput.Instance;
	}
}