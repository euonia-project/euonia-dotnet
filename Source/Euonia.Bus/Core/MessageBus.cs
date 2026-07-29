using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents the core message bus implementation that handles message routing, dispatching, and pipeline processing.
/// </summary>
/// <remarks>
/// The <see cref="MessageBus"/> class provides a centralized mechanism for publishing events, sending commands, 
/// and making request-response calls. It supports multiple transport mechanisms, pipeline behaviors for cross-cutting 
/// concerns, and message routing through dispatcher services.
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Multicast message publishing (pub/sub pattern)</description></item>
/// <item><description>Unicast message sending (command pattern)</description></item>
/// <item><description>Request-response calls (RPC pattern)</description></item>
/// <item><description>Configurable pipeline behaviors for message processing</description></item>
/// <item><description>Support for multiple transport mechanisms</description></item>
/// <item><description>Automatic message tracing and correlation</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class MessageBus : IBus
{
	/// <summary>
	/// Logger instance for recording message bus operations and diagnostics.
	/// </summary>
	private readonly ILogger<MessageBus> _logger;

	/// <summary>
	/// Dispatcher responsible for determining which transports to use for a given message type.
	/// </summary>
	private readonly IDispatcher _dispatcher;

	/// <summary>
	/// Optional accessor for retrieving the current request context (e.g., HTTP request information).
	/// </summary>
	private readonly IRequestContextAccessor _requestAccessor;

	/// <summary>
	/// Service provider for resolving dependencies and transport implementations.
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// Configuration options for the message bus, including default transport and pipeline settings.
	/// </summary>
	private readonly IMessageBusOptions _options;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageBus"/> class.
	/// </summary>
	/// <param name="dispatcher">The dispatcher for determining message transports.</param>
	/// <param name="provider">The service provider for dependency resolution.</param>
	/// <param name="options">The message bus options.</param>
	/// <param name="logger">The logger factory for creating loggers.</param>
	public MessageBus(IDispatcher dispatcher, IServiceProvider provider, IMessageBusOptions options, ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<MessageBus>();
		_dispatcher = dispatcher;
		_options = options;
		_provider = provider;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageBus"/> class with request context support.
	/// </summary>
	/// <param name="dispatcher">The dispatcher for determining message transports.</param>
	/// <param name="provider">The service provider for dependency resolution.</param>
	/// <param name="options">The message convention for type classification.</param>
	/// <param name="logger">The logger factory for creating loggers.</param>
	/// <param name="requestAccessor">The accessor for retrieving current request context.</param>
	public MessageBus(IDispatcher dispatcher, IServiceProvider provider, IMessageBusOptions options, ILoggerFactory logger, IRequestContextAccessor requestAccessor)
		: this(dispatcher, provider, options, logger)
	{
		_requestAccessor = requestAccessor;
	}

	/// <summary>
	/// Publishes a multicast message to all registered subscribers through configured transports.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to publish.</typeparam>
	/// <param name="message">The message instance to publish.</param>
	/// <param name="behavior">Optional action to configure pipeline behaviors for this publish operation.</param>
	/// <param name="options">Publishing options including channel name and message identifiers.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous publish operation.</returns>
	/// <exception cref="MessageTypeException">Thrown when the message type is not classified as a multicast type.</exception>
	/// <exception cref="MessageTransportException">Thrown when a configured transport is not registered.</exception>
	/// <remarks>
	/// This method validates the message type, creates a routed message with tracking identifiers,
	/// optionally processes the message through a pipeline, and publishes it to all determined transports in parallel.
	/// </remarks>
	public async Task PublishAsync<TMessage>(TMessage message, Action<PipelineMessage<IRoutedMessage, Unit>> behavior, PublishOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		options ??= new PublishOptions();

		var messageType = message.GetType();

		if (!_options.Convention.IsMulticastType(messageType))
		{
			throw new MessageTypeException("The message type is not an multicast type.");
		}

		var context = _requestAccessor?.Context;

		var channelName = options.Channel ?? MessageCache.Default.GetOrAddChannel(messageType);
		var pack = new RoutedMessage<TMessage, Unit>(message, channelName)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		metadataSetter?.Invoke(pack.Metadata);

		if (CheckPipelineBehaviorEnabled(options))
		{
			var pipeline = _provider.GetRequiredService<IPipeline<IRoutedMessage, Unit>>();

			var pipelineMessage = new PipelineMessage<IRoutedMessage, Unit>(pack, pipeline);

			if (options.AttachDefaultPipelineBehaviors)
			{
				pipelineMessage.Pipeline.UseOf(messageType, true);
				var behaviorTypes = MessageBusPipelineBehaviorTypeCache.Instance.GetOrAdd<Unit>(_provider);

				foreach (var behaviorType in behaviorTypes)
				{
					pipelineMessage.Pipeline.Use(behaviorType);
				}
			}

			if (behavior != null)
			{
				behavior(pipelineMessage);
			}

			await pipelineMessage.ExecuteAsync();
			pack = (RoutedMessage<TMessage, Unit>)pipelineMessage.Message;
		}

		var transports = _dispatcher.Determine(messageType);

		var tasks = new List<Task>();

		foreach (var name in transports)
		{
			_logger.LogDebug("Publishing message of type {MessageType} to transport {TransportType} on channel {ChannelName} with MessageId {MessageId}.",
				messageType.FullName, name, channelName, pack.MessageId);
			var transport = _provider.GetKeyedService<ITransport>(name);
			if (transport == null)
			{
				throw new MessageTransportException($"The transport '{name}' is not registered.");
			}

			tasks.Add(transport.PublishAsync(pack, cancellationToken));
		}

		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Sends a unicast message or request to a single handler through the configured transport.
	/// </summary>
	/// <typeparam name="TMessage">The type of message to send.</typeparam>
	/// <typeparam name="TResult">The type of result expected from the message handler.</typeparam>
	/// <param name="message">The message instance to send.</param>
	/// <param name="callback">Optional reactive subject to receive the result or errors asynchronously.</param>
	/// <param name="behavior">Optional action to configure pipeline behaviors for this send operation.</param>
	/// <param name="options">Send options including channel name, message identifiers, and correlation ID.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous send operation.</returns>
	/// <exception cref="MessageTypeException">Thrown when the message type is not classified as unicast or request type.</exception>
	/// <exception cref="MessageTransportException">Thrown when the configured transport is not registered.</exception>
	/// <remarks>
	/// This method validates the message type, creates a routed message with correlation tracking,
	/// optionally processes the message through a pipeline, and sends it to the first determined transport.
	/// Results or exceptions are propagated through the callback subject if provided.
	/// </remarks>
	public async Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, Action<PipelineMessage<IRoutedMessage, TResult>> behavior, SendOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		options ??= new SendOptions();

		var messageType = message.GetType();

		if (!_options.Convention.IsUnicastType(messageType))
		{
			throw new MessageTypeException("The message type is not a unicast type.");
		}

		var context = _requestAccessor?.Context;

		var channelName = options.Channel ?? MessageCache.Default.GetOrAddChannel(messageType);
		var pack = new RoutedMessage<TMessage, TResult>(message, channelName)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			CorrelationId = options.CorrelationId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		metadataSetter?.Invoke(pack.Metadata);

		if (CheckPipelineBehaviorEnabled(options))
		{
			var pipeline = _provider.GetRequiredService<IPipeline<IRoutedMessage, TResult>>();

			var pipelineMessage = new PipelineMessage<IRoutedMessage, TResult>(pack, pipeline);

			if (options.AttachDefaultPipelineBehaviors)
			{
				pipelineMessage.Pipeline.UseOf(messageType, true);

				var behaviorTypes = MessageBusPipelineBehaviorTypeCache.Instance.GetOrAdd<TResult>(_provider);

				foreach (var behaviorType in behaviorTypes)
				{
					pipelineMessage.Pipeline.Use(behaviorType);
				}
			}

			behavior?.Invoke(pipelineMessage);

			await pipelineMessage.ExecuteAsync();
			pack = (RoutedMessage<TMessage, TResult>)pipelineMessage.Message;
		}

		var transports = _dispatcher.Determine(messageType);

		var transportName = transports!.First();

		_logger.LogDebug("Publishing message of type {MessageType} to transport {TransportType} on channel {ChannelName} with MessageId {MessageId}.",
			messageType.FullName, transportName, channelName, pack.MessageId);

		var transport = _provider.GetKeyedService<ITransport>(transportName);

		if (transport == null)
		{
			throw new MessageTransportException($"The transport '{transportName}' is not registered.");
		}

		await transport.SendAsync(pack, cancellationToken)
		               .ContinueWith(task =>
		               {
			               task.WaitAndUnwrapException();
			               if (task.IsFaulted)
			               {
				               if (callback != null)
				               {
					               callback.OnError(task.Exception.GetBaseException());
				               }
				               else
				               {
					               throw task.Exception;
				               }
			               }
			               else
			               {
				               callback?.OnNext(task.Result);
			               }

			               if (task.IsCanceled)
			               {
				               callback?.OnCompleted();
			               }
		               }, cancellationToken);
	}

	/// <summary>
	/// Executes a request-response call and returns the result directly.
	/// </summary>
	/// <typeparam name="TResult">The type of result expected from the request handler.</typeparam>
	/// <param name="message">The request message implementing <see cref="IRequest{TResult}"/>.</param>
	/// <param name="behavior">Optional action to configure pipeline behaviors for this call operation.</param>
	/// <param name="options">Call options including channel name, message identifiers, and correlation ID.</param>
	/// <param name="metadataSetter">Optional action to configure message metadata.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation with the result from the handler.</returns>
	/// <exception cref="MessageTypeException">Thrown when the message type is not classified as a request type.</exception>
	/// <exception cref="MessageTransportException">Thrown when the configured transport is not registered.</exception>
	/// <remarks>
	/// This method is similar to <see cref="SendAsync{TMessage, TResult}"/> but directly returns the result
	/// instead of using a callback mechanism. It validates the request type, creates a routed message,
	/// optionally processes it through a pipeline, and sends it to the first determined transport.
	/// </remarks>
	public async Task<TResult> CallAsync<TResult>(IRequest<TResult> message, Action<PipelineMessage<IRoutedMessage, TResult>> behavior, CallOptions options, Action<MessageMetadata> metadataSetter = null, CancellationToken cancellationToken = default)
	{
		options ??= new CallOptions();

		var messageType = message.GetType();

		if (!_options.Convention.IsRequestType(messageType))
		{
			throw new MessageTypeException("The message type is not a request type.");
		}

		var context = _requestAccessor?.Context;

		var channelName = options.Channel ?? MessageCache.Default.GetOrAddChannel(messageType);
		var pack = new RoutedMessage<IRequest<TResult>, TResult>(message, channelName)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			CorrelationId = options.CorrelationId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		metadataSetter?.Invoke(pack.Metadata);

		if (CheckPipelineBehaviorEnabled(options))
		{
			var pipeline = _provider.GetRequiredService<IPipeline<IRoutedMessage, TResult>>();

			var pipelineMessage = new PipelineMessage<IRoutedMessage, TResult>(pack, pipeline);

			if (options.AttachDefaultPipelineBehaviors)
			{
				pipelineMessage.Pipeline.UseOf(messageType, true);
				var behaviorTypes = MessageBusPipelineBehaviorTypeCache.Instance.GetOrAdd<TResult>(_provider);

				foreach (var behaviorType in behaviorTypes)
				{
					pipelineMessage.Pipeline.Use(behaviorType);
				}
			}

			if (behavior != null)
			{
				behavior(pipelineMessage);
			}

			await pipelineMessage.ExecuteAsync();
			pack = (RoutedMessage<IRequest<TResult>, TResult>)pipelineMessage.Message;
		}

		var transports = _dispatcher.Determine(messageType);

		var transportName = transports!.First();

		_logger.LogDebug("Publishing message of type {MessageType} to transport {TransportType} on channel {ChannelName} with MessageId {MessageId}.",
			messageType.FullName, transportName, channelName, pack.MessageId);

		var transport = _provider.GetKeyedService<ITransport>(transportName);

		if (transport == null)
		{
			throw new MessageTransportException($"The transport '{transportName}' is not registered.");
		}

		var result = await transport.SendAsync(pack, cancellationToken)
		                            .ContinueWith(task =>
		                            {
			                            task.WaitAndUnwrapException();
			                            return task.Result;
		                            }, cancellationToken);
		return result;
	}

	/// <summary>
	/// Determines whether pipeline behaviors should be enabled for the current operation.
	/// </summary>
	/// <param name="options">The operation-specific options that may override the global setting.</param>
	/// <returns><c>true</c> if pipeline behaviors are enabled; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method checks if the operation-specific options have an explicit pipeline behavior setting.
	/// If not specified, it falls back to the global message bus configuration.
	/// </remarks>
	private bool CheckPipelineBehaviorEnabled(ExtendableOptions options)
	{
		if (options.EnablePipelineBehaviors.HasValue)
		{
			return options.EnablePipelineBehaviors.Value;
		}

		return _options.EnablePipelineBehaviors;
	}
}

/// <summary>
/// Caches the types of pipeline behaviors for different response types to optimize retrieval.
/// </summary>
internal class MessageBusPipelineBehaviorTypeCache
{
	private readonly ConcurrentDictionary<Type, List<Type>> _cache = new();

	public static MessageBusPipelineBehaviorTypeCache Instance => Singleton<MessageBusPipelineBehaviorTypeCache>.Get(() => new());

	public List<Type> GetOrAdd<TResponse>(IServiceProvider provider)
	{
		return _cache.GetOrAdd(typeof(TResponse), _ =>
		{
			return provider.GetServices<IPipelineBehavior<IRoutedMessage, TResponse>>()
			               .Select(b => b.GetType())
			               .Distinct()
			               .ToList();
		});
	}
}