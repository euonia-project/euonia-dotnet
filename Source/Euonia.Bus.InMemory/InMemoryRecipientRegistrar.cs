using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 内存消息接收者注册器。
/// 负责根据消息注册元数据以及配置的消息约定和传输策略，注册内存接收者（队列消费者或主题订阅者）。
/// </summary>
public sealed class InMemoryRecipientRegistrar : IRecipientRegistrar
{
	/// <summary>
	/// 内存总线的配置选项（包括传输名称和行为）。
	/// </summary>
	private readonly InMemoryBusOptions _options;

	/// <summary>
	/// 用于判断单播/多播/请求类型消息的命名与分类约定。
	/// </summary>
	private readonly IMessageConvention _convention;

	/// <summary>
	/// 用于解析接收者实现以及其他服务的服务提供程序。
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// 用于允许或禁止特定消息类型传入处理的传输策略。
	/// </summary>
	private readonly ITransportStrategy _strategy;

	/// <summary>
	/// 此注册器使用的日志记录器。
	/// </summary>
	private readonly ILogger<InMemoryRecipientRegistrar> _logger;

	/// <summary>
	/// 按接收者类型复用的接收者实例（当 <see cref="InMemoryBusOptions.MultipleSubscriberInstance"/> 为 <c>false</c> 时）。
	/// </summary>
	/// <remarks>
	/// 以 <see cref="Lazy{T}"/> 包装：<see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey, Func{TKey,TValue})"/>
	/// 不保证工厂只执行一次，直接在其中创建接收者会在并发注册时产生多个实例并全部注册到同一通道，
	/// 导致单播消息被重复处理。
	/// </remarks>
	private readonly ConcurrentDictionary<Type, Lazy<object>> _recipients = new();

	/// <summary>
	/// 持有所有已创建的接收者实例，防止多播订阅者因仅被弱引用信使引用而被提前回收。
	/// </summary>
	private readonly ConcurrentQueue<object> _aliveRecipients = new();

	/// <summary>
	/// 初始化 <see cref="InMemoryRecipientRegistrar"/> 类的新实例。
	/// </summary>
	/// <param name="configurator">提供约定和策略解析的消息总线配置器。</param>
	/// <param name="provider">用于创建接收者并解析依赖项的服务提供程序。</param>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的 <see cref="InMemoryBusOptions"/> 配置。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public InMemoryRecipientRegistrar(IConfigurator configurator, IServiceProvider provider, IOptions<InMemoryBusOptions> options, ILoggerFactory logger)
	{
		_options = options.Value;
		_convention = configurator.Convention;
		_provider = provider;
		_strategy = configurator.GetStrategy(_options.Name);
		_logger = logger.CreateLogger<InMemoryRecipientRegistrar>();
	}

	/// <summary>
	/// 为提供的消息注册信息注册消息接收者。
	/// 对于每个注册信息，此方法将：
	/// - 验证传输策略是否允许传入处理（当默认传输不同时）。
	/// - 根据消息约定解析相应的接收者实现：
	///   单播 -> <c>InMemoryQueueConsumer</c>，
	///   多播 -> <c>InMemoryTopicSubscriber</c>，
	///   请求 -> <c>InMemoryQueueConsumer</c>。
	/// - 将接收者注册到对应通道的信使上。
	/// </summary>
	/// <param name="registrations">要注册的 <see cref="ChannelRegistration"/> 实例集合。</param>
	/// <param name="defaultTransporter">默认传输的名称，用于判断是否需要应用传输策略。</param>
	/// <param name="cancellationToken">用于取消注册过程的令牌。</param>
	/// <returns>表示异步注册操作的任务。</returns>
	/// <exception cref="MessageTypeException">当消息类型不符合队列/主题/请求约定时抛出。</exception>
	public async Task RegisterAsync(IDictionary<string, ChannelRegistration> registrations, string defaultTransporter, CancellationToken cancellationToken = default)
	{
		foreach (var (channel, registration) in registrations)
		{
			_logger.LogDebug("[InMemoryRecipientRegistrar] Registering {MessageType} on channel {Channel}", registration.MessageType.FullName, channel);
			if (!string.Equals(defaultTransporter, _options.Name, StringComparison.OrdinalIgnoreCase))
			{
				if (_strategy == null || !_strategy.Incoming(channel, registration.MessageType))
				{
					continue;
				}
			}

			if (_convention.IsUnicast(channel, registration.MessageType))
			{
				var recipient = GetRecipient<InMemoryConsumer>();
				StrongReferenceMessenger.Default.Register(recipient, channel);
				_logger.LogInformation("[InMemoryRecipientRegistrar] Registering {MessageType} as unicast type on channel {Channel}", registration.MessageType.FullName, channel);
			}
			else if (_convention.IsMulticast(channel, registration.MessageType))
			{
				var recipient = GetRecipient<InMemorySubscriber>();
				WeakReferenceMessenger.Default.Register(recipient, channel);
				_logger.LogInformation("[InMemoryRecipientRegistrar] Registering {MessageType} as multicast type on channel {Channel}", registration.MessageType.FullName, channel);
			}
			else if (_convention.IsRequest(channel, registration.MessageType))
			{
				var recipient = GetRecipient<InMemoryExecutor>();
				StrongReferenceMessenger.Default.Register(recipient, channel);
				_logger.LogInformation("[InMemoryRecipientRegistrar] Registering {MessageType} as request type on channel {Channel}", registration.MessageType.FullName, channel);
			}
			else
			{
				throw new MessageTypeException($"The message type {registration.MessageType.AssemblyQualifiedName} is not a queue/topic/request type.");
			}
		}

		// 解析或复用接收者实例的辅助方法。
		// 如果允许每个订阅者创建多个实例（_options.MultipleSubscriberInstance == true），
		// 则每次都从服务提供程序返回新实例。否则按接收者类型在本注册器的 ConcurrentDictionary 中
		// 存储单例并复用。所有已创建的实例都会被强引用持有，防止多播订阅者被提前回收。
		TRecipient GetRecipient<TRecipient>()
			where TRecipient : InMemoryRecipient<TRecipient>, IRecipient
		{
			var recipient = _options.MultipleSubscriberInstance
				? _provider.GetRequiredService<TRecipient>()
				: (TRecipient)_recipients.GetOrAdd(typeof(TRecipient), type => new Lazy<object>(() => _provider.GetRequiredService(type))).Value;

			_aliveRecipients.Enqueue(recipient);
			return recipient;
		}

		await Task.CompletedTask;
	}

	/// <summary>
	/// 注销本注册器创建的接收者，解除其对信使的订阅。
	/// </summary>
	/// <remarks>
	/// 只影响本注册器创建的接收者，不会像此前的进程级 <c>Reset()</c> 那样清空其他内存总线实例的注册。
	/// </remarks>
	public ValueTask DisposeAsync()
	{
		while (_aliveRecipients.TryDequeue(out var recipient))
		{
			StrongReferenceMessenger.Default.UnregisterAll(recipient);
			WeakReferenceMessenger.Default.UnregisterAll(recipient);
		}

		_recipients.Clear();
		return ValueTask.CompletedTask;
	}
}