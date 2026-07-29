using System.Reactive.Subjects;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines the core message bus interface for publishing, sending, and calling messages.
/// </summary>
/// <remarks>
/// This interface provides asynchronous methods for different message patterns:
/// - Publish: Fire-and-forget messaging to multiple subscribers
/// - Send: Point-to-point messaging with optional response
/// - Call: Request-response pattern for retrieving results
/// </remarks>
public interface IBus
{
	/// <summary>
	/// Publishes a message to all subscribers with default options.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish. Must be a reference type.</typeparam>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="cancellationToken">Token to cancel the publish operation.</param>
	/// <returns>A task that represents the asynchronous publish operation.</returns>
	Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return PublishAsync(message, behavior: null, cancellationToken);
	}

	/// <summary>
	/// Publishes a message to all subscribers with default options.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish. Must be a reference type.</typeparam>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="behavior">Action to configure the pipeline message before publishing.</param>
	/// <param name="cancellationToken">Token to cancel the publish operation.</param>
	/// <returns>A task that represents the asynchronous publish operation.</returns>
	Task PublishAsync<TMessage>(TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return PublishAsync(message, behavior, new PublishOptions(), null, cancellationToken);
	}

	/// <summary>
	/// Publishes a message to all subscribers with specified options and metadata.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish. Must be a reference type.</typeparam>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="behavior">Action to configure the pipeline message before publishing.</param>
	/// <param name="options">Options to control the publishing behavior.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Token to cancel the publish operation.</param>
	/// <returns>A task that represents the asynchronous publish operation.</returns>
	Task PublishAsync<TMessage>(TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, PublishOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// Publishes a message to a specific channel with default options.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish. Must be a reference type.</typeparam>
	/// <param name="channel">The channel name to publish the message to.</param>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="cancellationToken">Token to cancel the publish operation.</param>
	/// <returns>A task that represents the asynchronous publish operation.</returns>
	Task PublishAsync<TMessage>(string channel, TMessage message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return PublishAsync(channel, message, behavior: null, cancellationToken);
	}

	/// <summary>
	/// Publishes a message to a specific channel with default options.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish. Must be a reference type.</typeparam>
	/// <param name="channel">The channel name to publish the message to.</param>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="behavior">Action to configure the pipeline message before publishing.</param>
	/// <param name="cancellationToken">Token to cancel the publish operation.</param>
	/// <returns>A task that represents the asynchronous publish operation.</returns>
	Task PublishAsync<TMessage>(string channel, TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return PublishAsync(channel, message, behavior, null, cancellationToken);
	}

	/// <summary>
	/// Publishes a message to a specific channel with metadata configuration.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish. Must be a reference type.</typeparam>
	/// <param name="channel">The channel name to publish the message to.</param>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="behavior">Action to configure the pipeline message before publishing.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Token to cancel the publish operation.</param>
	/// <returns>A task that represents the asynchronous publish operation.</returns>
	Task PublishAsync<TMessage>(string channel, TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return PublishAsync(message, behavior, new PublishOptions { Channel = channel }, metadataSetter, cancellationToken);
	}

	/// <summary>
	/// Sends a message to a single handler with default options and no response expected.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send. Must be a reference type.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="cancellationToken">Token to cancel the send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return SendAsync(message, behavior: null, cancellationToken);
	}

	/// <summary>
	/// Sends a message to a single handler with default options and no response expected.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send. Must be a reference type.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="behavior">Action to configure the pipeline message before sending.</param>
	/// <param name="cancellationToken">Token to cancel the send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	Task SendAsync<TMessage>(TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return SendAsync(message, behavior, new SendOptions(), null, cancellationToken);
	}

	/// <summary>
	/// Sends a message to a single handler with specified options and metadata, with no response expected.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send. Must be a reference type.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="behavior">Action to configure the pipeline message before sending.</param>
	/// <param name="options">Options to control the send behavior.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Token to cancel the send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	Task SendAsync<TMessage>(TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, SendOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return SendAsync(message, null, behavior, options, metadataSetter, cancellationToken);
	}

	/// <summary>
	/// Sends a message to a single handler with default options and processes the response via callback.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send. Must be a reference type.</typeparam>
	/// <typeparam name="TResult">The type of result expected from the handler.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="callback">Action to process the result received from the handler.</param>
	/// <param name="cancellationToken">Token to cancel the send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return SendAsync(message, callback, behavior: null, cancellationToken);
	}

	/// <summary>
	/// Sends a message to a single handler with default options and processes the response via callback.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send. Must be a reference type.</typeparam>
	/// <typeparam name="TResult">The type of result expected from the handler.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="callback">Action to process the result received from the handler.</param>
	/// <param name="behavior">Action to configure the pipeline message before sending.</param>
	/// <param name="cancellationToken">Token to cancel the send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, Action<PipelineMessage<IRoutedMessage, TResult>> behavior, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		return SendAsync(message, callback, behavior, new SendOptions(), null, cancellationToken);
	}

	/// <summary>
	/// Sends a message to a single handler with specified options and processes the response via callback.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send. Must be a reference type.</typeparam>
	/// <typeparam name="TResult">The type of result expected from the handler.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="callback">Action to process the result received from the handler.</param>
	/// <param name="behavior">Action to configure the pipeline message before sending.</param>
	/// <param name="options">Options to control the send behavior.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Token to cancel the send operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, Action<PipelineMessage<IRoutedMessage, TResult>> behavior, SendOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// Calls a request handler and returns the result with default options.
	/// </summary>
	/// <typeparam name="TResult">The type of result expected from the request handler.</typeparam>
	/// <param name="message">The request message implementing <see cref="IRequest{TResult}"/>.</param>
	/// <param name="cancellationToken">Token to cancel the call operation.</param>
	/// <returns>A task that represents the asynchronous call operation, containing the result.</returns>
	Task<TResult> CallAsync<TResult>(IRequest<TResult> message, CancellationToken cancellationToken = default)
	{
		return CallAsync(message, behavior: null, new CallOptions(), null, cancellationToken);
	}

	/// <summary>
	/// Calls a request handler and returns the result with default options.
	/// </summary>
	/// <typeparam name="TResult">The type of result expected from the request handler.</typeparam>
	/// <param name="message">The request message implementing <see cref="IRequest{TResult}"/>.</param>
	/// <param name="behavior">Action to configure the pipeline message before calling.</param>
	/// <param name="cancellationToken">Token to cancel the call operation.</param>
	/// <returns>A task that represents the asynchronous call operation, containing the result.</returns>
	Task<TResult> CallAsync<TResult>(IRequest<TResult> message, Action<PipelineMessage<IRoutedMessage, TResult>> behavior, CancellationToken cancellationToken = default)
	{
		return CallAsync(message, behavior, new CallOptions(), null, cancellationToken);
	}

	/// <summary>
	/// Calls a request handler and returns the result with specified options and metadata.
	/// </summary>
	/// <typeparam name="TResult">The type of result expected from the request handler.</typeparam>
	/// <param name="message">The request message implementing <see cref="IRequest{TResult}"/>.</param>
	/// <param name="behavior">Action to configure the pipeline message before calling.</param>
	/// <param name="options">Options to control the call behavior.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Token to cancel the call operation.</param>
	/// <returns>A task that represents the asynchronous call operation, containing the result.</returns>
	Task<TResult> CallAsync<TResult>(IRequest<TResult> message, Action<PipelineMessage<IRoutedMessage, TResult>> behavior, CallOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default);
}