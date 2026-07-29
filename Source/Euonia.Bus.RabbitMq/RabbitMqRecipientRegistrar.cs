using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// The RabbitMQ recipient registrar.
/// Responsible for creating and starting RabbitMQ recipients (queue consumers or topic subscribers)
/// based on message registration metadata and the configured message conventions and transport strategy.
/// </summary>
public sealed class RabbitMqRecipientRegistrar : IRecipientRegistrar
{
    /// <summary>
    /// The message naming and classification convention used to determine unicast/multicast/request types.
    /// </summary>
    private readonly IMessageConvention _convention;

    /// <summary>
    /// The service provider used to resolve recipient implementations and other services.
    /// </summary>
    private readonly IServiceProvider _provider;

    /// <summary>
    /// The transport strategy that can enable/disable incoming handling for specific message types.
    /// </summary>
    private readonly ITransportStrategy _strategy;

    /// <summary>
    /// The RabbitMQ bus options for the current registrar instance (includes the transport name).
    /// </summary>
    private readonly RabbitMqBusOptions _options;

    /// <summary>
    /// Logger instance for this registrar.
    /// </summary>
    private readonly ILogger<RabbitMqRecipientRegistrar> _logger;

    /// <summary>
    /// Initialize a new instance of <see cref="RabbitMqRecipientRegistrar"/>.
    /// </summary>
    /// <param name="configurator">The message bus configurator that provides conventions and strategy resolution.</param>
    /// <param name="provider">The service provider used to create recipients and resolve dependencies.</param>
    /// <param name="options">The configured <see cref="RabbitMqBusOptions"/> wrapped in <see cref="IOptions{T}"/>.</param>
    /// <param name="logger">The logger factory used to create a typed logger for this registrar.</param>
    public RabbitMqRecipientRegistrar(IMessageBusOptions configurator, IServiceProvider provider, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
    {
        _convention = configurator.Convention;
        _provider = provider;
        _options = options.Value;
        _strategy = configurator.GetStrategy(_options.Name);
        _logger = logger.CreateLogger<RabbitMqRecipientRegistrar>();
    }

    /// <summary>
    /// Register message recipients for the provided message registrations and start them.
    /// For each registration this method:
    /// - Verifies transport strategy allows incoming handling (when the default transport differs).
    /// - Resolves the appropriate recipient implementation based on message convention:
    ///   unicast -> <c>RabbitMqQueueConsumer</c>,
    ///   multicast -> <c>RabbitMqTopicSubscriber</c>,
    ///   request -> <c>RabbitMqQueueConsumer</c>.
    /// - Starts the recipient on the registration's channel.
    /// </summary>
    /// <param name="registrations">A collection of <see cref="MessageRegistration"/> instances to register.</param>
    /// <param name="defaultTransport">The name of the default transport; used to decide whether to apply the transport strategy.</param>
    /// <param name="cancellationToken">Token used to cancel the registration process.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    /// <exception cref="MessageTypeException">Thrown when a message type does not match queue/topic/request conventions.</exception>
    public async Task RegisterAsync(IEnumerable<MessageRegistration> registrations, string defaultTransport, CancellationToken cancellationToken = default)
    {
        foreach (var registration in registrations)
        {
            if (!string.Equals(defaultTransport, _options.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                // Check if the strategy allows incoming handling for the message type
                if (_strategy != null && !_strategy.Incoming(registration.MessageType))
                {
                    return;
                }
            }

            RabbitMqQueueRecipient recipient;
            if (_convention.IsUnicastType(registration.MessageType))
            {
                recipient = ActivatorUtilities.GetServiceOrCreateInstance<RabbitMqQueueConsumer>(_provider);
                _logger.LogInformation("[RabbitMqRecipientRegistrar] Registering {MessageType} as unicast type on channel {Channel}", registration.MessageType.FullName, registration.Channel);
            }
            else if (_convention.IsMulticastType(registration.MessageType))
            {
                recipient = ActivatorUtilities.GetServiceOrCreateInstance<RabbitMqTopicSubscriber>(_provider);
                _logger.LogInformation("[RabbitMqRecipientRegistrar] Registering {MessageType} as multicast type on channel {Channel}", registration.MessageType.FullName, registration.Channel);
            }
            else if (_convention.IsRequestType(registration.MessageType))
            {
                recipient = ActivatorUtilities.GetServiceOrCreateInstance<RabbitMqQueueConsumer>(_provider);
                _logger.LogInformation("[RabbitMqRecipientRegistrar] Registering {MessageType} as request type on channel {Channel}", registration.MessageType.FullName, registration.Channel);
            }
            else
            {
                throw new MessageTypeException($"The message type {registration.MessageType.AssemblyQualifiedName} is not a queue/topic/request type.");
            }

            await recipient.StartAsync(registration.Channel);
        }
    }
}