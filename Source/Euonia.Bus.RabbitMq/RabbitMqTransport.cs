using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// The <see cref="ITransport"/> implementation using RabbitMQ.
/// </summary>
public class RabbitMqTransport : ITransport
{
	/// <inheritdoc />
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	private readonly RabbitMqBusOptions _options;
	private readonly IPersistentConnection _connection;
	private readonly ILogger<RabbitMqTransport> _logger;

	/// <summary>
	/// Gets the transport name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Initialize a new instance of <see cref="RabbitMqTransport"/>.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="options"></param>
	/// <param name="logger"></param>
	public RabbitMqTransport(IPersistentConnection connection, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<RabbitMqTransport>();
		_connection = connection;
		_options = options.Value;
		Name = _options.Name ?? nameof(RabbitMqTransport);
	}

	/// <inheritdoc />
	public async Task PublishAsync<TMessage>(RoutedMessage<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		await using var channel = await _connection.CreateChannelAsync();

		var typeName = message.GetTypeName();

		var props = new BasicProperties();
		props.Headers ??= new Dictionary<string, object>();
		props.Headers[Constants.MessageHeaders.MessageType] = typeName;
		props.Type = typeName;

		await Policy.Handle<SocketException>()
		            .Or<BrokerUnreachableException>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(3), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            })
		            .ExecuteAsync(async () =>
		            {
			            var messageBody = await SerializeAsync(message, cancellationToken);

			            var exchangePrefix = string.Collapse(_options.ExchangeNamePrefix, Constants.DefaultExchangeNamePrefix);
			            var exchangeName = $"{exchangePrefix}:{message.Channel}";

			            await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, cancellationToken: cancellationToken);
			            await channel.BasicPublishAsync(exchangeName, $"{exchangeName}@{_options.RoutingKey}", true, props, messageBody, cancellationToken: cancellationToken);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Data, null));
		            });
	}

	/// <inheritdoc />
	public async Task SendAsync<TMessage>(RoutedMessage<TMessage> message, CancellationToken cancellationToken = default) where TMessage : class
	{
		var task = new TaskCompletionSource<dynamic>();

		var requestQueueName = GetQueueName(message.Channel);

		await using var channel = await _connection.CreateChannelAsync();

		await CheckQueueAsync(channel, requestQueueName);

		var responseQueueName = await channel.QueueDeclareAsync(cancellationToken: cancellationToken).ContinueWith(t => t.Result.QueueName, cancellationToken);
		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += OnReceived;

		var typeName = message.GetTypeName();

		var props = new BasicProperties
		{
			Type = typeName,
			CorrelationId = message.CorrelationId,
			ReplyTo = responseQueueName
		};
		props.Headers ??= new Dictionary<string, object>();
		props.Headers[Constants.MessageHeaders.MessageType] = typeName;

		await Policy.Handle<SocketException>()
		            .Or<BrokerUnreachableException>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(1), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            })
		            .ExecuteAsync(async () =>
		            {
			            var messageBody = await SerializeAsync(message, cancellationToken);
			            await channel.BasicPublishAsync("", requestQueueName, true, props, messageBody, cancellationToken);
			            await channel.BasicConsumeAsync(responseQueueName, true, consumer, cancellationToken: cancellationToken);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Data, null));
		            });

		await task.Task;
		consumer.ReceivedAsync -= OnReceived;

		async Task OnReceived(object sender, BasicDeliverEventArgs args)
		{
			if (args.BasicProperties.CorrelationId != message.CorrelationId)
			{
				return;
			}

			var body = args.Body.ToArray();
			var response = JsonConvert.DeserializeObject<RabbitMqReply<object>>(Encoding.UTF8.GetString(body), Constants.SerializerSettings);
			if (response.IsSuccess)
			{
				task.SetResult(response.Result);
			}
			else
			{
				task.SetException(response.Error);
			}

			await Task.CompletedTask;
		}
	}

	/// <inheritdoc />
	public async Task<TResponse> SendAsync<TMessage, TResponse>(RoutedMessage<TMessage, TResponse> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		var task = new TaskCompletionSource<TResponse>();

		var requestQueueName = GetQueueName(message.Channel);

		await using var channel = await _connection.CreateChannelAsync();

		await CheckQueueAsync(channel, requestQueueName);

		var responseQueueName = (await channel.QueueDeclareAsync(cancellationToken: cancellationToken)).QueueName;
		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += OnReceivedAsync;

		var typeName = message.GetTypeName();

		var props = new BasicProperties
		{
			Type = typeName,
			CorrelationId = message.CorrelationId,
			ReplyTo = responseQueueName
		};
		props.Headers ??= new Dictionary<string, object>();
		props.Headers[Constants.MessageHeaders.MessageType] = typeName;

		await Policy.Handle<SocketException>()
		            .Or<BrokerUnreachableException>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(1), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            })
		            .ExecuteAsync(async () =>
		            {
			            var messageBody = await SerializeAsync(message, cancellationToken);
			            await channel.BasicPublishAsync("", requestQueueName, true, props, messageBody, cancellationToken);
			            await channel.BasicConsumeAsync(responseQueueName, true, consumer, cancellationToken: cancellationToken);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Data, null));
		            });

		var result = await task.Task;
		consumer.ReceivedAsync -= OnReceivedAsync;
		return result;

		async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
		{
			if (args.BasicProperties.CorrelationId != message.CorrelationId)
			{
				return;
			}

			var body = args.Body.ToArray();

			if (typeof(TResponse).IsIn(typeof(Unit), typeof(Task), typeof(ValueTask), typeof(void)))
			{
				var response = JsonConvert.DeserializeObject<RabbitMqReply<object>>(Encoding.UTF8.GetString(body), Constants.SerializerSettings);
				if (response.IsSuccess)
				{
					task.SetResult(default);
				}
				else
				{
					task.SetException(response.Error);
				}
			}
			else
			{
				var response = JsonConvert.DeserializeObject<RabbitMqReply<TResponse>>(Encoding.UTF8.GetString(body), Constants.SerializerSettings);
				if (response.IsSuccess)
				{
					task.SetResult(response.Result);
				}
				else
				{
					task.SetException(response.Error);
				}
			}

			await Task.CompletedTask;
		}
	}

	/// <summary>
	/// Builds the queue name for the specified channel.
	/// </summary>
	/// <param name="channel">The channel name.</param>
	/// <returns>The queue name.</returns>
	private string GetQueueName(string channel)
	{
		var subscriptionId = string.Collapse(_options.SubscriptionId, Assembly.GetEntryAssembly()?.FullName, channel);
		var requestQueueName = $"{string.Collapse(_options.QueueNamePrefix, Constants.DefaultQueueNamePrefix)}:{channel}@{subscriptionId}";
		return requestQueueName;
	}

	private static async Task CheckQueueAsync(IChannel channel, string requestQueueName)
	{
		try
		{
			var queueDeclare = await channel.QueueDeclarePassiveAsync(requestQueueName);

			if (queueDeclare == null)
			{
				throw new MessageDeliverException("Channel not found in vhost '/'.");
			}

			if (queueDeclare.ConsumerCount < 1)
			{
				throw new MessageDeliverException("No consumer found for the channel.");
			}
		}
		catch (OperationInterruptedException exception) when (exception.ShutdownReason?.ReplyCode == 404)
		{
			throw new MessageDeliverException("No consumer found for the channel.");
		}
	}

	/// <summary>
	/// Serializes the message to bytes.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="message"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private static async Task<byte[]> SerializeAsync<TMessage>(RoutedMessage<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		if (message == null)
		{
			return Array.Empty<byte>();
		}

		await using var stream = new MemoryStream();
		// The default UTF8Encoding emits the BOM will cause the RabbitMQ client to fail to deserialize the message.
		await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
		{
			await using var jsonWriter = new JsonTextWriter(writer);

			JsonSerializer.CreateDefault(Constants.SerializerSettings)
			              .Serialize(jsonWriter, message);

			await jsonWriter.FlushAsync(cancellationToken);
			await writer.FlushAsync(cancellationToken);
		}

		return stream.ToArray();
	}
}