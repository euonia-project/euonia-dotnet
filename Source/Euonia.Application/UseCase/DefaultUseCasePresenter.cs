namespace Nerosoft.Euonia.Application;

/// <summary>
/// The default implementation of <see cref="IUseCasePresenter{TOutput}"/>.
/// </summary>
/// <typeparam name="TOutput"></typeparam>
public class DefaultUseCasePresenter<TOutput> : DisposableObject,
												IUseCasePresenter<TOutput>
{
	/// <inheritdoc/>
	public event EventHandler<TOutput>? OnSucceed;

	/// <inheritdoc/>
	public event EventHandler<Exception>? OnFailed;

	/// <inheritdoc/>
	public event EventHandler? OnCanceled;

	/// <inheritdoc/>
	public void Error(Exception exception)
	{
		if (exception is OperationCanceledException)
		{
			OnCanceled?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			OnFailed?.Invoke(this, exception);
		}
	}

	/// <inheritdoc/>
	public void Ok(TOutput output)
	{
		Output = output;
		OnSucceed?.Invoke(this, output);
	}

	/// <summary>
	/// Gets the output of the use case.
	/// </summary>
	public TOutput Output { get; private set; } = default!;

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		OnSucceed = null;
		OnFailed = null;
		OnCanceled = null;
	}
}