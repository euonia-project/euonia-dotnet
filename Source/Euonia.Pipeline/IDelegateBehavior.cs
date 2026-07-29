namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// Delegate behavior to surround the inner handler.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IDelegateBehavior<in TRequest>
{
	/// <summary>
	/// Handle the request.
	/// </summary>
	/// <param name="request"></param>
	/// <param name="next"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
    Task HandleAsync(TRequest request, Delegate next, CancellationToken cancellationToken);
}