namespace Nerosoft.Euonia.Application;

/// <summary>
/// Defines the structure of use case execute response presenter.
/// </summary>
/// <typeparam name="TOutput"></typeparam>
public interface IUseCasePresenter<TOutput> : IUseCaseOutputFailure,
											  IUseCaseOutputSuccess<TOutput>,
											  IDisposable
{
	/// <summary>
	/// Occurs when the use case is successfully executed.
	/// </summary>
	event EventHandler<TOutput>? OnSucceed;

	/// <summary>
	/// Occurs when the use case is failed to execute.
	/// </summary>
	event EventHandler<Exception>? OnFailed;

	/// <summary>
	/// Occurs when the use case is canceled.
	/// </summary>
	event EventHandler? OnCanceled;
}