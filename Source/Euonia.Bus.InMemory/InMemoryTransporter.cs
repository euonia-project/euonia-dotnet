using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 基于内存消息传递的 <see cref="ITransporter"/> 实现。
/// </summary>
public class InMemoryTransporter : DisposableObject, ITransporter
{
	/// <summary>
	/// 获取传输器的名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 当消息被分发时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	private readonly ILogger<InMemoryTransporter> _logger;
	private readonly WeakReferenceMessenger _weakMessenger;
	private readonly StrongReferenceMessenger _strongMessenger;

	/// <summary>
	/// 用于在发布时读取发布方请求上下文；手动构造时未提供。
	/// </summary>
	private readonly IServiceAccessor _accessor;

	/// <summary>
	/// 初始化 <see cref="InMemoryTransporter"/> 类的新实例。
	/// </summary>
	/// <param name="options">内存总线选项。</param>
	/// <param name="logger">日志工厂。</param>
	public InMemoryTransporter(IOptions<InMemoryBusOptions> options, ILoggerFactory logger)
		: this(options, logger, new InMemoryMessengerProvider())
	{
	}

	/// <summary>
	/// 初始化 <see cref="InMemoryTransporter"/> 类的新实例。
	/// </summary>
	/// <param name="options">内存总线选项。</param>
	/// <param name="logger">日志工厂。</param>
	/// <param name="messengers">与接收者注册器共享的信使实例。</param>
	public InMemoryTransporter(IOptions<InMemoryBusOptions> options, ILoggerFactory logger, InMemoryMessengerProvider messengers)
	{
		var opts = options.Value;
		Name = opts.Name ?? Constants.DefaultTransportName;
		_logger = logger.CreateLogger<InMemoryTransporter>();
		_weakMessenger = messengers.Weak;
		_strongMessenger = messengers.Strong;
	}

	/// <summary>
	/// 初始化 <see cref="InMemoryTransporter"/> 类的新实例，并在发布时保留发布方请求上下文。
	/// </summary>
	/// <param name="options">内存总线选项。</param>
	/// <param name="logger">日志工厂。</param>
	/// <param name="messengers">与接收者注册器共享的信使实例。</param>
	/// <param name="accessor">用于读取发布方请求上下文的访问器。</param>
	public InMemoryTransporter(IOptions<InMemoryBusOptions> options, ILoggerFactory logger, InMemoryMessengerProvider messengers, IServiceAccessor accessor)
		: this(options, logger, messengers)
	{
		_accessor = accessor;
	}

	/// <summary>
	/// 发布（多播）消息，通过弱引用信使发送给所有订阅者。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型，必须是引用类型。</typeparam>
	/// <param name="message">要发布的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	public async Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		var context = CreateMessageContext(message);
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};
		_weakMessenger.Send(pack, message.Channel);
		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));
		await Task.CompletedTask;
	}

	/// <summary>
	/// 发送（单播）消息，通过强引用信使发送并等待处理完成。
	/// 监听上下文的 <see cref="MessageContext.Failed"/> 和 <see cref="MessageContext.Completed"/> 事件以确定处理结果。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型，必须是引用类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	public async Task SendAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		// 调用方会等待处理完成，发布方作用域在整条链路中存活，处理时直接复用。
		var context = CreateMessageContext(message, GetHandlingServiceProvider());
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};

		var taskCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		CancellationTokenRegistration cancellationRegistration = default;
		if (cancellationToken != CancellationToken.None)
		{
			cancellationRegistration = cancellationToken.Register(() => taskCompletion.TrySetCanceled(cancellationToken));
		}

		context.Failed += (_, exception) =>
		{
			taskCompletion.TrySetException(exception);
		};

		context.Completed += (_, _) =>
		{
			taskCompletion.TrySetResult();
		};

		_strongMessenger.UnsafeSend(pack, message.Channel);

		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));

		try
		{
			await taskCompletion.Task;
		}
		finally
		{
			cancellationRegistration.Dispose();
		}
	}

	/// <summary>
	/// 发送（单播）消息并等待处理程序返回响应结果。
	/// 通过 <see cref="MessageContext.Responded"/> 事件接收响应，通过 <see cref="MessageContext.Failed"/> 接收异常。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResponse">响应的类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	public async Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		// 调用方会等待处理完成，发布方作用域在整条链路中存活，处理时直接复用。
		using var context = CreateMessageContext(message, GetHandlingServiceProvider());
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};

		// See https://stackoverflow.com/questions/18760252/timeout-an-async-method-implemented-with-taskcompletionsource
		var taskCompletion = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
		CancellationTokenRegistration cancellationRegistration = default;
		if (cancellationToken != CancellationToken.None)
		{
			cancellationRegistration = cancellationToken.Register(() => taskCompletion.TrySetCanceled(cancellationToken), false);
		}

		try
		{
			context.Responded += OnResponded;
			context.Failed += OnFailed;
			context.Completed += OnCompleted;

			_strongMessenger.UnsafeSend(pack, message.Channel);
			Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));

			var result = await taskCompletion.Task;
			return result;
		}
		finally
		{
			cancellationRegistration.Dispose();
			context.Responded -= OnResponded;
			context.Failed -= OnFailed;
			context.Completed -= OnCompleted;
		}

		void OnResponded(object sender, MessageRepliedEventArgs args)
		{
			_logger.LogDebug("Message '{MessageId}' responded with result: {Result}", message.MessageId, args.Result);
			taskCompletion.TrySetResult((TResponse)args.Result);
		}

		void OnFailed(object sender, Exception exception)
		{
			_logger.LogError(exception, "Message '{MessageId}' ({MessageType}) on channel '{Channel}' failed with exception", message.MessageId, message.TypeName, message.Channel);
			taskCompletion.TrySetException(exception);
		}

		void OnCompleted(object sender, MessageHandledEventArgs args)
		{
			_logger.LogDebug("Message '{MessageId}' completed", message.MessageId);
			taskCompletion.TryCompleteFromCompletedTask(Task.FromResult(default(TResponse)));
		}
	}

	/// <summary>
	/// 调用请求消息并等待返回响应结果。
	/// 与 <see cref="SendAsync{TMessage, TResponse}"/> 逻辑相同，用于请求-响应模式的语义化方法。
	/// </summary>
	/// <typeparam name="TRequest">请求消息负载的类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResponse">响应的类型，必须是引用类型。</typeparam>
	/// <param name="message">请求消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		// 调用方会等待处理完成，发布方作用域在整条链路中存活，处理时直接复用。
		using var context = CreateMessageContext(message, GetHandlingServiceProvider());
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};

		// See https://stackoverflow.com/questions/18760252/timeout-an-async-method-implemented-with-taskcompletionsource
		var taskCompletion = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
		CancellationTokenRegistration cancellationRegistration = default;
		if (cancellationToken != CancellationToken.None)
		{
			cancellationRegistration = cancellationToken.Register(() => taskCompletion.TrySetCanceled(cancellationToken), false);
		}

		context.Responded += OnResponded;
		context.Failed += OnFailed;
		context.Completed += OnCompleted;

		try
		{
			_strongMessenger.UnsafeSend(pack, message.Channel);
			Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));

			return await taskCompletion.Task;
		}
		finally
		{
			cancellationRegistration.Dispose();
			context.Responded -= OnResponded;
			context.Failed -= OnFailed;
			context.Completed -= OnCompleted;
		}

		void OnResponded(object sender, MessageRepliedEventArgs args)
		{
			_logger.LogDebug("Message '{MessageId}' responded with result: {Result}", message.MessageId, args.Result);
			taskCompletion.TrySetResult((TResponse)args.Result);
		}

		void OnFailed(object sender, Exception exception)
		{
			_logger.LogError(exception, "Message '{MessageId}' ({MessageType}) on channel '{Channel}' failed with exception", message.MessageId, message.TypeName, message.Channel);
			taskCompletion.TrySetException(exception);
		}

		void OnCompleted(object sender, MessageHandledEventArgs args)
		{
			_logger.LogDebug("Message '{MessageId}' completed", message.MessageId);
			taskCompletion.TryCompleteFromCompletedTask(Task.FromResult(default(TResponse)));
		}
	}

	/// <summary>
	/// 创建消息上下文，并捕获发布方请求上下文快照。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <param name="handlingServiceProvider">处理时复用的服务提供程序；多播发布时为 <c>null</c>。</param>
	/// <returns>携带发布方上下文快照的消息上下文。</returns>
	private MessageContext CreateMessageContext(IMessageEnvelope message, IServiceProvider handlingServiceProvider = null)
	{
		return new MessageContext(message)
		{
			PublisherContext = CapturePublisherContext(message.MessageId),
			HandlingServiceProvider = handlingServiceProvider,
		};
	}

	/// <summary>
	/// 获取当前调用方显式绑定的服务提供程序，供单播/请求处理复用。
	/// </summary>
	/// <returns>调用方的服务提供程序；未显式绑定或传输器为手动构造时返回 <c>null</c>。</returns>
	private IServiceProvider GetHandlingServiceProvider()
	{
		return _accessor != null && _accessor.TryGetServiceProvider(out var provider)
			       ? provider
			       : null;
	}

	/// <summary>
	/// 捕获当前发布方的请求上下文；网络传输不经过该路径。
	/// </summary>
	/// <param name="messageId">消息标识符，用于记录捕获失败的上下文。</param>
	/// <returns>发布方上下文快照；手动构造的传输器不做捕获。</returns>
	private PublisherContextSnapshot CapturePublisherContext(string messageId)
	{
		if (_accessor == null)
		{
			return PublisherContextSnapshot.NotCaptured;
		}

		if (!_accessor.TryGetServiceProvider(out var provider))
		{
			// 发布方没有显式绑定服务提供程序，视为没有请求上下文；
			// 仍然标记为已捕获，以便处理时清除后台泵继承自其他消息的上下文。
			return PublisherContextSnapshot.Capture(null);
		}

		try
		{
			return PublisherContextSnapshot.Capture(provider.GetService<DefaultRequestContextAccessor>()?.Context);
		}
		catch (ObjectDisposedException exception)
		{
			// 发布方作用域已释放：消息仍可投递，但无法保留发布方上下文。
			_logger.LogWarning(exception, "Publisher scope was already disposed while capturing context for message {MessageId}; publisher context will not be preserved.", messageId);
			return PublisherContextSnapshot.Capture(null);
		}
	}

	/// <summary>
	/// 释放传输器自身持有的资源。
	/// </summary>
	/// <param name="disposing">指示是否正在主动释放资源。</param>
	/// <remarks>
	/// 本类型不持有需要释放的资源。此前这里会调用
	/// <c>StrongReferenceMessenger.Default.Reset()</c> 与 <c>WeakReferenceMessenger.Default.Reset()</c>：
	/// 这两个信使是进程级单例，因此释放任意一个传输器实例都会清空进程内所有内存总线的注册，
	/// 且因为忽略了 <paramref name="disposing"/>，终结器线程同样会触发该副作用。
	/// 接收者的注销由创建它们的 <see cref="InMemoryRecipientRegistrar"/> 在释放时负责，
	/// 不会影响其他传输器实例。
	/// </remarks>
	protected override void Dispose(bool disposing)
	{
	}
}