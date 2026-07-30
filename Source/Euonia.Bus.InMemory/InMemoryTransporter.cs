using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

	/// <summary>
	/// 初始化 <see cref="InMemoryTransporter"/> 类的新实例。
	/// </summary>
	/// <param name="options">内存总线选项。</param>
	/// <param name="logger">日志工厂。</param>
	public InMemoryTransporter(IOptions<InMemoryBusOptions> options, ILoggerFactory logger)
	{
		var opts = options.Value;
		Name = opts.Name ?? nameof(InMemoryTransporter);
		_logger = logger.CreateLogger<InMemoryTransporter>();
	}

	/// <summary>
	/// 发布（多播）消息，通过弱引用信使发送给所有订阅者。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型，必须是引用类型。</typeparam>
	/// <param name="message">要发布的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	public async Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		var context = new MessageContext(message);
		var pack = new MessagePack(message, context)
		{
			Aborted = cancellationToken
		};
		WeakReferenceMessenger.Default.Send(pack, message.Channel);
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

		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));

		await taskCompletion.Task;
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
		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));

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

	/// <summary>
	/// 调用请求消息并等待返回响应结果。
	/// 与 <see cref="SendAsync{TMessage, TResponse}"/> 逻辑相同，用于请求-响应模式的语义化方法。
	/// </summary>
	/// <typeparam name="TRequest">请求消息负载的类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResponse">响应的类型，必须是引用类型。</typeparam>
	/// <param name="message">请求消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
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
		Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, context));

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

	/// <summary>
	/// 释放资源时重置强引用和弱引用信使的状态。
	/// </summary>
	/// <param name="disposing">指示是否正在主动释放资源。</param>
	protected override void Dispose(bool disposing)
	{
		StrongReferenceMessenger.Default.Reset();
		WeakReferenceMessenger.Default.Reset();
	}
}