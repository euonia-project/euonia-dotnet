using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// A pipeline behavior that validates message data before processing.
/// </summary>
/// <typeparam name="TMessage">The type of message being processed. Must be a class that implements <see cref="IRoutedMessage"/>.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the pipeline.</typeparam>
/// <remarks>
/// This behavior intercepts messages in the pipeline and validates their data using the configured <see cref="IValidator"/>.
/// If validation fails, an exception will be thrown before the next pipeline delegate is invoked.
/// </remarks>
public class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IRoutedMessage
{
	/// <summary>
	/// Handles the message by validating its data and then invoking the next behavior in the pipeline.
	/// </summary>
	/// <param name="context">The message context containing the data to validate.</param>
	/// <param name="next">The next delegate in the pipeline to invoke after successful validation.</param>
	/// <returns>A task that represents the asynchronous operation, containing the response from the pipeline.</returns>
	/// <exception cref="ValidationException">Thrown when validation fails.</exception>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		await Validator.ValidateAsync(context.Data);
		return await next(context);
	}
}