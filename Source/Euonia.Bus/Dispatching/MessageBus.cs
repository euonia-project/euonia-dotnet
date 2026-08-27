using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus.Behaviors;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 核心消息总线的实现，负责消息路由、分发和管道处理。
/// </summary>
/// <remarks>
/// <see cref="MessageBus"/> 类提供了用于发布事件、发送命令和进行请求-响应调用的集中式机制。
/// 支持多种传输机制、用于横切关注点的管道行为，以及通过分发器服务进行消息路由。
/// <para>
/// 主要功能：
/// <list type="bullet">
/// <item><description>多播消息发布（发布/订阅模式）</description></item>
/// <item><description>单播消息发送（命令模式）</description></item>
/// <item><description>请求-响应调用（RPC 模式）</description></item>
/// <item><description>可配置的消息处理管道行为</description></item>
/// <item><description>支持多种传输机制</description></item>
/// <item><description>自动消息追踪和关联</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class MessageBus : IBus
{
	/// <summary>
	/// 负责确定给定消息类型应使用哪些传输器的分发器。
	/// </summary>
	private readonly IDispatcher _dispatcher;

	/// <summary>
	/// 用于检索当前请求上下文（例如 HTTP 请求信息）的可选访问器。
	/// </summary>
	private readonly IRequestContextAccessor _requestAccessor;

	/// <summary>
	/// 用于依赖解析的服务访问器，提供对服务提供程序的访问。
	/// </summary>
	private readonly IServiceAccessor _accessor;

	/// <summary>
	/// 消息总线配置器，提供消息约定、注册信息和默认传输器配置。
	/// </summary>
	private readonly IConfigurator _configurator;

	/// <summary>
	/// 已注册的传输器集合，按名称索引，用于实际的消息传输操作。
	/// </summary>
	private readonly ConcurrentDictionary<string, ITransporter> _transporters = new();

	/// <summary>
	/// 初始化 <see cref="MessageBus"/> 类的新实例，并支持请求上下文。
	/// </summary>
	/// <param name="accessor">用于依赖解析的服务访问器。</param>
	/// <param name="configurator">消息总线设置的配置器。</param>
	/// <param name="dispatcher">用于确定消息传输的分发器。</param>
	/// <param name="requestAccessor">用于检索当前请求上下文的访问器。</param>
	public MessageBus(IServiceAccessor accessor, IConfigurator configurator, IDispatcher dispatcher, IRequestContextAccessor requestAccessor)
	{
		_dispatcher = dispatcher;
		_accessor = accessor;
		_configurator = configurator;
		_requestAccessor = requestAccessor;
	}

	/// <summary>
	/// 通过已配置的传输器将多播消息发布给所有已注册的订阅者。
	/// </summary>
	/// <typeparam name="TMessage">要发布的消息类型。</typeparam>
	/// <param name="message">要发布的消息实例。</param>
	/// <param name="behavior">用于为此发布操作配置管道行为的可选委托。</param>
	/// <param name="options">发布选项，包括通道名称和消息标识符。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	/// <exception cref="MessageTypeException">当消息类型未归类为多播类型时抛出。</exception>
	/// <exception cref="MessageTransportException">当已配置的传输器未注册时抛出。</exception>
	/// <remarks>
	/// 此方法验证消息类型，创建带有追踪标识符的路由消息，可选择性地通过管道处理消息，
	/// 并将其并行发布到所有确定的传输器。
	/// </remarks>
	public Task PublishAsync<TMessage>(TMessage message, PublishOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, Unit>> behavior, CancellationToken cancellationToken = default)
	{
		options ??= new PublishOptions();

		var messageType = message.GetType();
		var channel = GetChannel(messageType, options);
		if (!_configurator.Convention.IsMulticast(channel, messageType))
		{
			throw new MessageTypeException("The message type is not a multicast type.");
		}

		var context = _requestAccessor?.Context;

		var pack = new RoutedMessage<TMessage, Unit>(message, channel)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		options.MetadataSetter?.Invoke(pack.Metadata);

		var transports = _dispatcher.Determine(channel, messageType);

		return Parallel.ForEachAsync(transports, cancellationToken, async (name, token) =>
		{
			await RunWithPipelineAsync(pack, behavior, (transport, p) =>
			{
				return transport.PublishAsync(p, token).ContinueWith(_ => Unit.Value, token);
			}, name);
		});
	}

	/// <summary>
	/// 通过已配置的传输器将单播消息或请求发送给单个处理程序。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
	/// <typeparam name="TResult">期望从消息处理程序返回的结果类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="callback">用于异步接收结果或错误的可选响应式 Subject。</param>
	/// <param name="behavior">用于为此发送操作配置管道行为的可选委托。</param>
	/// <param name="options">发送选项，包括通道名称、消息标识符和关联 ID。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	/// <exception cref="MessageTypeException">当消息类型未归类为单播或请求类型时抛出。</exception>
	/// <exception cref="MessageTransportException">当已配置的传输器未注册时抛出。</exception>
	/// <remarks>
	/// 此方法验证消息类型，创建带有关联追踪的路由消息，可选择性地通过管道处理消息，
	/// 并将其发送到第一个确定的传输器。结果或异常通过回调 Subject 进行传播（如果已提供）。
	/// </remarks>
	public async Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, SendOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, CancellationToken cancellationToken = default)
	{
		options ??= new SendOptions();

		var messageType = message.GetType();
		var channel = GetChannel(messageType, options);
		if (!_configurator.Convention.IsUnicast(channel, messageType))
		{
			throw new MessageTypeException("The message type is not a unicast type.");
		}

		var context = _requestAccessor?.Context;

		var pack = new RoutedMessage<TMessage, TResult>(message, channel)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			CorrelationId = options.CorrelationId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		options.MetadataSetter?.Invoke(pack.Metadata);

		var transports = _dispatcher.Determine(channel, messageType);

		var transportName = transports!.First();

		try
		{
			var result = await RunWithPipelineAsync(pack, behavior, (transport, envelope) => transport.SendAsync<TMessage, TResult>(envelope, cancellationToken), transportName);
			callback?.OnNext(result);
		}
		catch (Exception exception)
		{
			if (callback != null)
			{
				callback.OnError(exception);
			}
			else
			{
				throw;
			}
		}
		finally
		{
			callback?.OnCompleted();
		}
	}

	/// <summary>
	/// 执行请求-响应调用并直接返回结果。
	/// </summary>
	/// <typeparam name="TResponse">期望从请求处理程序返回的结果类型。</typeparam>
	/// <typeparam name="TRequest">请求消息的类型。</typeparam>
	/// <param name="message">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="behavior">用于为此调用操作配置管道行为的可选委托。</param>
	/// <param name="options">调用选项，包括通道名称、消息标识符和关联 ID。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>表示异步操作的任务，包含来自处理程序的结果。</returns>
	/// <exception cref="MessageTypeException">当消息类型未归类为请求类型时抛出。</exception>
	/// <exception cref="MessageTransportException">当已配置的传输器未注册时抛出。</exception>
	/// <remarks>
	/// 此方法类似于 <see cref="SendAsync{TMessage, TResult}"/>，但直接返回结果而非使用回调机制。
	/// 验证请求类型，创建路由消息，可选择性地通过管道处理，并将其发送到第一个确定的传输器。
	/// </remarks>
	public Task<TResponse> CallAsync<TRequest, TResponse>(TRequest message, CallOptions options, Action<IPipeline<IMessageEnvelope<TRequest>, TResponse>> behavior, CancellationToken cancellationToken = default)
	{
		options ??= new CallOptions();

		var messageType = message.GetType();
		var channel = GetChannel(messageType, options);
		if (!_configurator.Convention.IsRequest(channel, messageType))
		{
			throw new MessageTypeException("The message type is not a request type.");
		}

		var context = _requestAccessor?.Context;

		var pack = new RoutedMessage<TRequest>(message, channel)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			CorrelationId = options.CorrelationId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		options.MetadataSetter?.Invoke(pack.Metadata);

		var transports = _dispatcher.Determine(channel, messageType);

		var transportName = transports!.First();

		return RunWithPipelineAsync(pack, behavior, (transport, p) => transport.CallAsync<TRequest, TResponse>(p, cancellationToken), transportName);
	}

	/// <summary>
	/// 使用指定的选项和管道行为调用实现了 <see cref="IRequest{TResult}"/> 的请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="request">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="options">调用选项，包括通道名称、消息标识符和关联 ID。</param>
	/// <param name="behavior">用于为此调用操作配置管道行为的可选委托。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>表示异步操作的任务，包含来自处理程序的结果。</returns>
	/// <exception cref="MessageTypeException">当消息类型未归类为请求类型时抛出。</exception>
	/// <exception cref="MessageTransportException">当已配置的传输器未注册时抛出。</exception>
	public Task<TResult> CallAsync<TResult>(IRequest<TResult> request, CallOptions options, Action<IPipeline<IMessageEnvelope<IRequest<TResult>>, TResult>> behavior, CancellationToken cancellationToken = default)
	{
		options ??= new CallOptions();
		var messageType = request.GetType();
		var channel = GetChannel(messageType, options);
		if (!_configurator.Convention.IsRequest(channel, messageType))
		{
			throw new MessageTypeException("The message type is not a request type.");
		}

		var context = _requestAccessor?.Context;
		var pack = new RoutedMessage<IRequest<TResult>>(request, channel)
		{
			MessageId = options.MessageId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			CorrelationId = options.CorrelationId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString(),
			RequestTraceId = context?.TraceIdentifier ?? options.RequestTraceId ?? ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N"),
			Authorization = context?.Authorization,
			User = context?.User,
		};

		options.MetadataSetter?.Invoke(pack.Metadata);

		var transports = _dispatcher.Determine(channel, messageType);

		var transportName = transports!.First();
		return RunWithPipelineAsync(pack, behavior, (transport, p) => transport.CallAsync<IRequest<TResult>, TResult>(p, cancellationToken), transportName);
	}

	/// <summary>
	/// 直接调用指定的处理程序委托，并传入服务提供程序和取消令牌。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="handler">用于处理请求的委托。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	public Task<TResult> CallAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler, CancellationToken cancellationToken = default)
	{
		return handler(_accessor.ServiceProvider);
	}

	/// <summary>
	/// 通过管道执行消息处理流程
	/// </summary>
	/// <remarks>
	/// 配置管道行为（日志记录和类型匹配），然后解析指定的传输器并调用后续委托完成实际的传输操作。
	/// </remarks>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <typeparam name="TResult">结果类型。</typeparam>
	/// <param name="pack">路由消息包。</param>
	/// <param name="behavior">用于配置管道的可选委托。</param>
	/// <param name="next">执行实际传输操作的委托。</param>
	/// <param name="transportName">要使用的传输器名称。</param>
	/// <returns>表示异步管道处理操作的任务，包含处理结果。</returns>
	private Task<TResult> RunWithPipelineAsync<TMessage, TResult>(RoutedMessage<TMessage> pack, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, Func<ITransporter, IMessageEnvelope<TMessage>, Task<TResult>> next, string transportName)
	{
		var pipeline = _accessor.GetRequiredService<IPipeline<IMessageEnvelope<TMessage>, TResult>>();

		pipeline.Use(typeof(OutgoingLoggingBehavior<TMessage, TResult>), transportName, _accessor.GetService<ILogger<MessageBus>>());
		pipeline.UseOf(pack.Payload.GetType(), true);

		behavior?.Invoke(pipeline);

		return pipeline.RunAsync(pack, async (message) =>
		{
			var transport = _transporters.GetOrAdd(transportName, name =>
			{
				var service = _accessor.GetKeyedService<ITransporter>(name);
				return service ?? throw new MessageTransportException($"The transport '{name}' is not registered.");
			});
			return await next(transport, message);
		});
	}

	/// <summary>
	/// 根据选项和消息类型获取通道名称，优先使用选项中指定的通道，否则使用默认消息通道。
	/// </summary>
	/// <param name="messageType">消息类型。</param>
	/// <param name="options">消息选项。</param>
	/// <returns>通道名称。</returns>
	private static string GetChannel(Type messageType, ExtendableOptions options)
	{
		if (!string.IsNullOrEmpty(options.Channel))
		{
			return options.Channel;
		}

		var channel = MessageCache.Default.GetOrAddChannel(messageType);

		return !string.IsNullOrWhiteSpace(channel) ? channel : throw new MessageDeliverException($"The channel name for message type '{messageType.FullName}' cannot be null or empty. Please specify a channel in the options or configure a default channel for this message type.");
	}
}