using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Threading;

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
	/// 接收消息包，触发 <see cref="MessageReceived"/> 事件后调用 <see cref="HandleAsync"/> 进行业务处理，
	/// 最后触发 <see cref="MessageAcknowledged"/> 事件。
	/// </summary>
	/// <param name="pack">消息包。</param>
	/// <exception cref="MessageProcessingException">当消息处理过程中发生错误时抛出。</exception>
	public void Receive(MessagePack pack)
	{
		try
		{
			MessageReceived?.Invoke(this, new MessageReceivedEventArgs(pack.Message, pack.Context));
			AsyncContext.Run(() => HandleAsync(pack.Message.Channel, pack.Message.Payload, pack.Context, pack.Aborted));
			MessageAcknowledged?.Invoke(this, new MessageAcknowledgedEventArgs(pack.Message, pack.Context));
		}
		catch (Exception e)
		{
			throw new MessageProcessingException(pack.Message.MessageId, "消息处理过程中发生错误", e);
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