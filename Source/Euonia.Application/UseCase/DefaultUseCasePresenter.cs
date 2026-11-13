namespace Nerosoft.Euonia.Application;

/// <summary>
/// <see cref="IUseCasePresenter{TOutput}"/> 的默认实现。
/// </summary>
/// <typeparam name="TOutput">用例执行结果（输出）的类型。</typeparam>
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
	/// 获取用例的输出。
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