using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 
/// </summary>
public class RabbitMqTopicSubscriber : RabbitMqQueueRecipient, ITopicSubscriber
{
	private readonly IHandlerContext _handler;
	private readonly ILogger<RabbitMqTopicSubscriber> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="RabbitMqTopicSubscriber"/> class.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="handler"></param>
	/// <param name="options"></param>
	/// <param name="logger"></param>
	public RabbitMqTopicSubscriber(IPersistentConnection connection, IHandlerContext handler, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
		: base(connection, options)
	{
		_handler = handler;
		_logger = logger.CreateLogger<RabbitMqTopicSubscriber>();
	}

	/// <inheritdoc />
	public string Name => nameof(RabbitMqTopicSubscriber);

	/// <summary>
	/// Gets the RabbitMQ message channel.
	/// </summary>
	private IChannel Channel { get; set; }

	/// <summary>
	/// Gets the RabbitMQ consumer instance.
	/// </summary>
	private AsyncEventingBasicConsumer Consumer { get; set; }

	internal override async Task StartAsync(string channel)
	{
		Channel = await Connection.CreateChannelAsync();

		var exchangePrefix = string.Collapse(Options.ExchangeNamePrefix, Constants.DefaultExchangeNamePrefix);
		var exchangeName = $"{exchangePrefix}:{channel}";

		// Declare Fanout exchange and queue for topic subscriber.
		// All messages published to the exchange will be routed to all queues bound to the exchange.
		await Channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout);

		// Each subscriber gets its own queue to receive messages,
		// all instances of the same subscriber will share the same queue.
		var subscriptionId = string.Collapse(Options.SubscriptionId, Assembly.GetEntryAssembly()?.FullName, channel);
		var queueName = await Channel.QueueDeclareAsync($"{exchangeName}@{subscriptionId}", true, false, false)
		                             .ContinueWith(task => task.Result.QueueName);

		Consumer = new AsyncEventingBasicConsumer(Channel);
		Consumer.ReceivedAsync += HandleMessageReceivedAsync;

		await Channel.QueueBindAsync(queueName, channel, Options.RoutingKey ?? "*");
		await Channel.BasicConsumeAsync(string.Empty, Options.AutoAck, Consumer);
	}

	/// <inheritdoc />
	protected override async Task HandleMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
	{
		var type = MessageTypeCache.GetMessageType(args.BasicProperties.Type);

		var message = DeserializeMessage(args.Body.ToArray(), type);

		var context = new MessageContext(message);

		OnMessageReceived(new MessageReceivedEventArgs(message.Data, context));

		await HandleAsync(message.Channel, message.Data, context);

		if (!Options.AutoAck)
		{
			await Channel.BasicAckAsync(args.DeliveryTag, false);
		}

		OnMessageAcknowledged(new MessageAcknowledgedEventArgs(message.Data, context));
	}

	/// <inheritdoc />
	protected override async Task HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			await _handler.HandleAsync(channel, message, context, cancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Message '{Id}' Handle Error: {Message}", context.MessageId, exception.Message);
		}
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Consumer.ReceivedAsync -= HandleMessageReceivedAsync;
		Channel?.Dispose();
	}
}