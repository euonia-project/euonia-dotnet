using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// The in-memory message recipient registrar.
/// Responsible for registering in-memory recipients (queue consumers or topic subscribers)
/// based on message registration metadata and the configured message conventions and transport strategy.
/// </summary>
public sealed class InMemoryRecipientRegistrar : IRecipientRegistrar
{
	/// <summary>
	/// The configured options for the in-memory bus (includes transport name and behavior).
	/// </summary>
	private readonly InMemoryBusOptions _options;

	/// <summary>
	/// Message naming and classification conventions used to determine unicast/multicast/request types.
	/// </summary>
	private readonly IMessageConvention _convention;

	/// <summary>
	/// Service provider used to resolve recipient implementations and other services.
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// Transport strategy used to allow or disallow incoming handling for specific message types.
	/// </summary>
	private readonly ITransportStrategy _strategy;

	/// <summary>
	/// Logger used by this registrar.
	/// </summary>
	private readonly ILogger<InMemoryRecipientRegistrar> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryRecipientRegistrar"/> class.
	/// </summary>
	/// <param name="configurator">The message bus configurator that provides conventions and strategy resolution.</param>
	/// <param name="provider">The service provider used to create recipients and resolve dependencies.</param>
	/// <param name="options">The configured <see cref="InMemoryBusOptions"/> wrapped in <see cref="IOptions{T}"/>.</param>
	/// <param name="logger">The logger factory used to create a typed logger for this registrar.</param>
	public InMemoryRecipientRegistrar(IMessageBusOptions configurator, IServiceProvider provider, IOptions<InMemoryBusOptions> options, ILoggerFactory logger)
	{
		_options = options.Value;
		_convention = configurator.Convention;
		_provider = provider;
		_strategy = configurator.GetStrategy(_options.Name);
		_logger = logger.CreateLogger<InMemoryRecipientRegistrar>();
	}

	/// <summary>
	/// Register message recipients for the provided message registrations.
	/// For each registration this method:
	/// - Verifies transport strategy allows incoming handling (when the default transport differs).
	/// - Resolves the appropriate recipient implementation based on message convention:
	///   unicast -> <c>InMemoryQueueConsumer</c>,
	///   multicast -> <c>InMemoryTopicSubscriber</c>,
	///   request -> <c>InMemoryQueueConsumer</c>.
	/// - Registers the recipient with the messenger for the registration's channel.
	/// </summary>
	/// <param name="registrations">A collection of <see cref="MessageRegistration"/> instances to register.</param>
	/// <param name="defaultTransport">The name of the default transport; used to decide whether to apply the transport strategy.</param>
	/// <param name="cancellationToken">Token used to cancel the registration process.</param>
	/// <returns>A task that represents the asynchronous registration operation.</returns>
	/// <exception cref="MessageTypeException">Thrown when a message type does not match queue/topic/request conventions.</exception>
	public async Task RegisterAsync(IEnumerable<MessageRegistration> registrations, string defaultTransport, CancellationToken cancellationToken = default)
	{
		var recipients = new ConcurrentDictionary<Type, InMemoryRecipient>();

		foreach (var registration in registrations)
		{
			if (!string.Equals(defaultTransport, _options.Name, StringComparison.CurrentCultureIgnoreCase))
			{
				if (_strategy == null || !_strategy.Incoming(registration.MessageType))
				{
					continue;
				}
			}

			if (_convention.IsUnicastType(registration.MessageType))
			{
				var recipient = GetRecipient<InMemoryQueueConsumer>();
				StrongReferenceMessenger.Default.Register(recipient, registration.Channel);
				_logger.LogInformation("[InMemoryRecipientRegistrar] Registering {MessageType} as unicast type on channel {Channel}", registration.MessageType.FullName, registration.Channel);
			}
			else if (_convention.IsMulticastType(registration.MessageType))
			{
				var recipient = GetRecipient<InMemoryTopicSubscriber>();
				WeakReferenceMessenger.Default.Register(recipient, registration.Channel);
				_logger.LogInformation("[InMemoryRecipientRegistrar] Registering {MessageType} as multicast type on channel {Channel}", registration.MessageType.FullName, registration.Channel);
			}
			else if (_convention.IsRequestType(registration.MessageType))
			{
				var recipient = GetRecipient<InMemoryQueueConsumer>();
				StrongReferenceMessenger.Default.Register(recipient, registration.Channel);
				_logger.LogInformation("[InMemoryRecipientRegistrar] Registering {MessageType} as request type on channel {Channel}", registration.MessageType.FullName, registration.Channel);
			}
			else
			{
				throw new MessageTypeException($"The message type {registration.MessageType.AssemblyQualifiedName} is not a queue/topic/request type.");
			}
		}

		// Helper to resolve or reuse recipient instances.
		// If multiple instances per subscriber are allowed (_options.MultipleSubscriberInstance == true),
		// a new instance from the provider is returned each time. Otherwise a singleton per recipient type
		// is stored in the local ConcurrentDictionary and reused.
		TRecipient GetRecipient<TRecipient>()
			where TRecipient : InMemoryRecipient, IRecipient
		{
			if (_options.MultipleSubscriberInstance)
			{
				return _provider.GetService<TRecipient>();
			}
			else
			{
				return (TRecipient)recipients.GetOrAdd(typeof(TRecipient), _ => _provider.GetService<TRecipient>());
			}
		}

		await Task.CompletedTask;
	}
}