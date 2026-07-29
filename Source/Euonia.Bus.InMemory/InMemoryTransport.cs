using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// The <see cref="ITransport"/> implementation using in-memory messaging.
/// </summary>
public class InMemoryTransport : DisposableObject, ITransport
{
	/// <summary>
	/// Gets the transport name.
	/// </summary>
	public string Name { get; }

	/// <inheritdoc />
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	private readonly ILogger<InMemoryTransport> _logger;

	/// <summary>
	/// Initialize a new instance of <see cref="InMemoryTransport"/>
	/// </summary>
	/// <param name="options"></param>
	/// <param name="logger"></param>
	public InMemoryTransport(IOptions<InMemoryBusOptions> options, ILoggerFactory logger)
	{
		var opts = options.Value;
		Name = opts.Name ?? nameof(InMemoryTransport);
		_logger = logger.CreateLogger<InMemoryTransport>();
	}

	/// <inheritdoc />
	public async Task PublishAsync<TMessage>(RoutedMessage<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		var context = new MessageContext(message);
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};
		WeakReferenceMessenger.Default.Send(pack, message.Channel);
		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Data, context));
		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task SendAsync<TMessage>(RoutedMessage<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		var context = new MessageContext(message);
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};

		var taskCompletion = new TaskCompletionSource();

		if (cancellationToken != CancellationToken.None)
		{
			cancellationToken.Register(() => taskCompletion.SetCanceled(cancellationToken));
		}

		context.Failed += (_, exception) =>
		{
			taskCompletion.TrySetException(exception);
		};

		context.Completed += (_, _) =>
		{
			taskCompletion.TrySetResult();
		};

		StrongReferenceMessenger.Default.UnsafeSend(pack, message.Channel);

		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Data, context));

		await taskCompletion.Task;
	}

	/// <inheritdoc />
	public async Task<TResponse> SendAsync<TMessage, TResponse>(RoutedMessage<TMessage, TResponse> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		using var context = new MessageContext(message);
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};

		// See https://stackoverflow.com/questions/18760252/timeout-an-async-method-implemented-with-taskcompletionsource
		var taskCompletion = new TaskCompletionSource<TResponse>();
		if (cancellationToken != CancellationToken.None)
		{
			cancellationToken.Register(() => taskCompletion.TrySetCanceled(), false);
		}

		context.Responded += OnResponded;
		context.Failed += OnFailed;
		context.Completed += OnCompleted;

		StrongReferenceMessenger.Default.UnsafeSend(pack, message.Channel);
		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Data, context));

		var result = await taskCompletion.Task;
		context.Responded -= OnResponded;
		context.Failed -= OnFailed;
		context.Completed -= OnCompleted;
		return result;

		void OnResponded(object sender, MessageRepliedEventArgs args)
		{
			_logger.LogDebug("Message '{MessageId}' responded with result: {Result}", message.MessageId, args.Result);
			taskCompletion.TrySetResult((TResponse)args.Result);
		}

		void OnFailed(object sender, Exception exception)
		{
			_logger.LogError(exception, "Message '{MessageId}' failed with exception", message.MessageId);
			taskCompletion.TrySetException(exception);
		}

		void OnCompleted(object sender, MessageHandledEventArgs args)
		{
			_logger.LogDebug("Message '{MessageId}' completed", message.MessageId);
			taskCompletion.TryCompleteFromCompletedTask(Task.FromResult(default(TResponse)));
		}
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		StrongReferenceMessenger.Default.Reset();
		WeakReferenceMessenger.Default.Reset();
	}
}