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
	/// 当前调用方作用域的服务访问器；手动构造时未提供。
	/// </summary>
	private readonly IServiceAccessor _accessor;

	/// <summary>
	/// 当前调用方作用域的请求上下文访问器；手动构造时未提供。
	/// </summary>
	private readonly IRequestContextAccessor _requestContextAccessor;

	/// <summary>
	/// 初始化 <see cref="InMemoryTransporter"/> 类的新实例。
	/// </summary>
	/// <param name="options">内存总线选项。</param>
	/// <param name="logger">日志工厂。</param>
	/// <param name="messengers">与接收者注册器共享的信使实例；为 <c>null</c> 时创建独立实例（手动构造场景）。</param>
	/// <param name="accessor">当前调用方作用域的服务访问器；为 <c>null</c> 时不复用调用方作用域。</param>
	/// <param name="requestContextAccessor">当前调用方作用域的请求上下文访问器；为 <c>null</c> 时不保留发布方上下文。</param>
	public InMemoryTransporter(
		IOptions<InMemoryBusOptions> options,
		ILoggerFactory logger,
		InMemoryMessengerProvider messengers = null,
		IServiceAccessor accessor = null,
		IRequestContextAccessor requestContextAccessor = null)
	{
		var opts = options.Value;
		Name = opts.Name ?? Constants.DefaultTransportName;
		_logger = logger.CreateLogger<InMemoryTransporter>();

		var provider = messengers ?? new InMemoryMessengerProvider();
		_weakMessenger = provider.Weak;
		_strongMessenger = provider.Strong;
		_accessor = accessor;
		_requestContextAccessor = requestContextAccessor;
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
		var context = CreateMessageContext(message, borrowCallerScope: false);
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
		var context = CreateMessageContext(message, borrowCallerScope: true);
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
		using var context = CreateMessageContext(message, borrowCallerScope: true);
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
		using var context = CreateMessageContext(message, borrowCallerScope: true);
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
	/// 创建消息上下文，并填写本次消息的执行环境描述符。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <param name="borrowCallerScope">
	/// <c>true</c> 表示单播/请求由调用方等待处理完成，可复用其显式绑定的作用域；
	/// <c>false</c> 表示多播发布（fire-and-forget），处理时创建新作用域。
	/// </param>
	/// <returns>携带执行环境描述符的消息上下文。</returns>
	private MessageContext CreateMessageContext(IMessageEnvelope message, bool borrowCallerScope)
	{
		return new MessageContext(message)
		{
			ExecutionDescriptor = CreateExecutionDescriptor(borrowCallerScope),
		};
	}

	/// <summary>
	/// 生成执行环境描述符；网络传输不经过该路径。
	/// </summary>
	/// <param name="borrowCallerScope">是否复用调用方作用域（单播/请求）。</param>
	/// <returns>本次消息的执行环境描述符。</returns>
	private MessageExecutionDescriptor CreateExecutionDescriptor(bool borrowCallerScope)
	{
		if (_accessor == null)
		{
			return MessageExecutionDescriptor.RemoteInbound;
		}

		// 访问器是作用域服务：其提供程序就是当前调用方作用域，调用方等待处理完成时必然存活。
		var callerProvider = _accessor.ServiceProvider;
		var publisherContext = _requestContextAccessor?.Context;

		return borrowCallerScope
			       ? MessageExecutionDescriptor.InProcessCall(callerProvider, publisherContext)
			       : MessageExecutionDescriptor.InProcessPublish(publisherContext);
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