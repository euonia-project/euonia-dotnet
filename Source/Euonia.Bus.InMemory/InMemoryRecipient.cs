using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 内存接收者的抽象基类，提供接收消息并委托给子类处理的通用逻辑。
/// </summary>
/// <typeparam name="TRecipient">具体的接收者子类类型，用于创建类型化日志记录器。</typeparam>
public abstract class InMemoryRecipient<TRecipient> : DisposableObject, IRecipient<MessagePack>
	where TRecipient : InMemoryRecipient<TRecipient>
{
	/// <summary>
	/// 初始化 <see cref="InMemoryRecipient{TRecipient}"/> 类的新实例。
	/// </summary>
	/// <param name="handler">用于业务处理的消息处理器上下文。</param>
	/// <param name="factory">用于创建类型化日志记录器的日志工厂。</param>
	protected InMemoryRecipient(IHandlerContext handler, ILoggerFactory factory)
	{
		Handler = handler;
		Logger = factory.CreateLogger<TRecipient>();
	}

	/// <summary>
	/// 获取消息处理器上下文。
	/// </summary>
	protected virtual IHandlerContext Handler { get; }

	/// <summary>
	/// 获取类型化日志记录器。
	/// </summary>
	protected virtual ILogger<TRecipient> Logger { get; }

	/// <summary>
	/// 获取接收者的名称。
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	/// 当消息被接收到时触发。
	/// </summary>
	public event EventHandler<MessageReceivedEventArgs> MessageReceived;

	/// <summary>
	/// 当消息处理完成并确认时触发。
	/// </summary>
	public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;

	#region IDisposable

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
	}

	#endregion

	/// <summary>
	/// 接收消息包，触发 <see cref="MessageReceived"/> 事件后交由后台泵异步处理，
	/// 处理完成后再触发 <see cref="MessageAcknowledged"/> 事件。
	/// </summary>
	/// <param name="pack">消息包。</param>
	/// <remarks>
	/// <see cref="IMessenger"/> 的接收契约是同步的（信使在同一线程上同步调用本方法），
	/// 因此这里不能直接 <c>await</c> 异步处理。此前的实现用 <c>AsyncContext.Run</c> 同步阻塞等待处理完成，
	/// 使得每次 <c>PublishAsync</c> 都会阻塞调用方线程直到所有处理程序执行完毕
	/// （在 ASP.NET Core 请求线程下存在饥饿与死锁风险）。
	/// <para>
	/// 现在改为投递到接收者自己的串行泵：同一接收者内仍按入队顺序逐个处理，
	/// 但 <c>PublishAsync</c> 不再等待处理完成。请求-响应（<c>SendAsync</c> / <c>CallAsync</c>）
	/// 仍然会等待，因为它们 await 的是处理程序写入结果的 <see cref="System.Threading.Tasks.TaskCompletionSource"/>。
	/// </para>
	/// </remarks>
	public void Receive(MessagePack pack)
	{
		MessageReceived?.Invoke(this, new MessageReceivedEventArgs(pack.Message, pack.Context));

		_pending.Enqueue(pack);
		EnsurePumpRunning();
	}

	/// <summary>
	/// 待处理的消息包，由 <see cref="PumpAsync"/> 按入队顺序逐个处理。
	/// </summary>
	private readonly ConcurrentQueue<MessagePack> _pending = new();

	/// <summary>
	/// 泵的运行标志（0 = 空闲，1 = 运行中），用于保证同一接收者内只有一个泵在消费队列。
	/// </summary>
	private int _pumping;

	/// <summary>
	/// 在泵未运行时启动它。
	/// </summary>
	private void EnsurePumpRunning()
	{
		if (Interlocked.CompareExchange(ref _pumping, 1, 0) == 0)
		{
			_ = Task.Run(PumpAsync);
		}
	}

	/// <summary>
	/// 串行消费待处理队列，直到队列为空。
	/// </summary>
	private async Task PumpAsync()
	{
		try
		{
			while (_pending.TryDequeue(out var pack))
			{
				try
				{
					await HandleAsync(pack.Message.Channel, pack.Message.Payload, pack.Context, pack.Aborted).ConfigureAwait(false);
				}
				catch (Exception exception)
				{
					// HandleAsync 自身已捕获处理程序异常并转为 context.Failure；
					// 这里仅兜底，避免后台任务出现未被观察到的异常。
					Logger.LogError(exception, "Message '{Id}' pump error: {Message}", pack.Context?.MessageId, exception.Message);
				}

				MessageAcknowledged?.Invoke(this, new MessageAcknowledgedEventArgs(pack.Message, pack.Context));
			}
		}
		finally
		{
			Interlocked.Exchange(ref _pumping, 0);

			// 置空闲后必须复查队列：否则与本次退出并发的 Enqueue 可能观察到"泵在运行"而不再启动泵，
			// 导致该消息永久滞留在队列中。
			if (!_pending.IsEmpty)
			{
				EnsurePumpRunning();
			}
		}
	}

	/// <summary>
	/// 处理接收到的消息，委托给 <see cref="IHandlerContext"/> 执行业务逻辑。
	/// 异常时会记录错误日志并通知 <see cref="MessageContext.Failure"/>，最终总是调用 <see cref="MessageContext.Complete(string)"/>。
	/// </summary>
	/// <param name="channel">消息通道。</param>
	/// <param name="message">消息负载。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示消息处理异步操作的任务。</returns>
	protected virtual async Task HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await Handler.HandleAsync(channel, message, context, cancellationToken);
			context.Response(result);
		}
		catch (Exception exception)
		{
			Logger.LogError(exception, "Message '{Id}' Handle Error: {Message}", context.MessageId, exception.Message);
			context.Failure(exception);
		}
		finally
		{
			context.Complete(null);
		}
	}
}