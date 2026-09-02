using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// RabbitMQ 接收器注册器。
/// 负责根据消息注册元数据以及已配置的消息约定和传输策略，
/// 创建并启动 RabbitMQ 接收器（队列消费者或主题订阅者）。
/// </summary>
internal sealed class RabbitMqRecipientRegistrar : IRecipientRegistrar
{
	/// <summary>
	/// 用于判断单播/多播/请求类型的消息命名与分类约定。
	/// </summary>
	private readonly IMessageConvention _convention;

	/// <summary>
	/// 用于解析接收器实现及其他服务的服务提供程序。
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// 可以对特定消息类型启用/禁用入站处理的传输策略。
	/// </summary>
	private readonly ITransportStrategy _strategy;

	/// <summary>
	/// 当前注册器实例的 RabbitMQ 总线选项（含传输器名称）。
	/// </summary>
	private readonly RabbitMqBusOptions _options;

	/// <summary>
	/// 当前注册器的日志记录器实例。
	/// </summary>
	private readonly ILogger<RabbitMqRecipientRegistrar> _logger;

	/// <summary>
	/// 初始化 <see cref="RabbitMqRecipientRegistrar"/> 的新实例。
	/// </summary>
	/// <param name="configurator">提供约定与策略解析的消息总线配置器。</param>
	/// <param name="provider">用于创建接收器并解析依赖项的服务提供程序。</param>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的已配置 <see cref="RabbitMqBusOptions"/>。</param>
	/// <param name="logger">用于创建当前注册器类型化日志记录器的日志工厂。</param>
	public RabbitMqRecipientRegistrar(IConfigurator configurator, IServiceProvider provider, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
	{
		_convention = configurator.Convention;
		_provider = provider;
		_options = options.Value;
		_strategy = configurator.GetStrategy(_options.Name);
		_logger = logger.CreateLogger<RabbitMqRecipientRegistrar>();
	}

	/// <summary>
	/// 为提供的消息注册信息注册消息接收器并启动它们。
	/// 对每个注册项，本方法：
	/// - 验证传输策略是否允许入站处理（当默认传输器不同时）；
	/// - 根据消息约定解析合适的接收器实现：
	///   单播 -> <c>RabbitMqQueueConsumer</c>，
	///   多播 -> <c>RabbitMqTopicSubscriber</c>，
	///   请求 -> <c>RabbitMqQueueConsumer</c>；
	/// - 在注册的通道上启动接收器。
	/// </summary>
	/// <param name="registrations">待注册的 <see cref="ChannelRegistration"/> 实例集合。</param>
	/// <param name="defaultTransporter">默认传输器名称；用于决定是否应用传输策略。</param>
	/// <param name="cancellationToken">用于取消注册过程的令牌。</param>
	/// <returns>表示异步注册操作的任务。</returns>
	/// <exception cref="MessageTypeException">当消息类型不匹配队列/主题/请求约定时抛出。</exception>
	public async Task RegisterAsync(IDictionary<string, ChannelRegistration> registrations, string defaultTransporter, CancellationToken cancellationToken = default)
	{
		foreach (var (channel, registration) in registrations)
		{
			_logger.LogInformation("[RabbitMqRecipientRegistrar] Registering {MessageType} on channel {Channel}", registration.MessageType.FullName, channel);
			if (!string.Equals(defaultTransporter, _options.Name, StringComparison.CurrentCultureIgnoreCase))
			{
				// 检查策略是否允许对该消息类型进行入站处理
				if (_strategy == null || !_strategy.Incoming(channel, registration.MessageType))
				{
					continue;
				}
			}

			var conventionType = _convention.Detect(channel, registration.MessageType);

			RabbitMqRecipient recipient = conventionType switch
			{
				MessageConventionType.Unicast => new RabbitMqConsumer(_provider, channel, registration.MessageType),
				MessageConventionType.Multicast => new RabbitMqSubscriber(_provider, channel, registration.MessageType),
				MessageConventionType.Request => new RabbitMqExecutor(_provider, channel, registration.MessageType),
				MessageConventionType.None => throw new MessageTypeException($"The message type {registration.MessageType.AssemblyQualifiedName} is not a queue/topic/request type."),
				_ => throw new MessageTypeException($"The message type {registration.MessageType.AssemblyQualifiedName} is not a queue/topic/request type.")
			};
			
			await recipient.StartAsync(cancellationToken);
		}
	}
}