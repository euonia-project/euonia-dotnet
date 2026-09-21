using System.Reactive.Subjects;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于流式配置并执行发送（单播）消息操作的构建器，支持通过 <see cref="Subject{TResult}"/> 接收响应。
/// </summary>
/// <typeparam name="TMessage">要发送的消息类型。</typeparam>
/// <typeparam name="TResult">期望从处理程序返回的结果类型。</typeparam>
public class SendBuilder<TMessage, TResult> : DispatchBuilder<SendBuilder<TMessage, TResult>, SendOptions, TMessage, TResult>
{
	private readonly IBus _bus;
	private readonly TMessage _message;
	private Subject<TResult> _subject;

	/// <summary>
	/// 初始化 <see cref="SendBuilder{TMessage, TResult}"/> 类的新实例。
	/// </summary>
	/// <param name="bus">消息总线实例。</param>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="options">发送消息的选项。</param>
	public SendBuilder(IBus bus, TMessage message, SendOptions options)
		: base(options)
	{
		_bus = bus;
		_message = message;
	}

	/// <summary>
	/// 设置关联标识符。
	/// </summary>
	/// <param name="correlationId">关联标识符。</param>
	/// <returns>返回当前的 <see cref="SendBuilder{TMessage, TResult}"/> 实例，以便进行链式调用。</returns>
	public SendBuilder<TMessage, TResult> WithCorrelationId(string correlationId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
		Options.CorrelationId = correlationId;
		return this;
	}

	/// <summary>
	/// 设置用于接收响应结果的 <see cref="Subject{TResult}"/> 回调对象。
	/// </summary>
	/// <param name="subject">用于接收响应的 Subject 对象。</param>
	/// <returns>返回当前的 <see cref="SendBuilder{TMessage, TResult}"/> 实例，以便进行链式调用。</returns>
	public SendBuilder<TMessage, TResult> WithCallback(Subject<TResult> subject)
	{
		_subject = subject;
		return this;
	}

	/// <summary>
	/// 使用已配置的选项和管道执行发送操作。
	/// </summary>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	public Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return _bus.SendAsync(_message, _subject, Options, Pipeline, cancellationToken);
	}

	/// <summary>
	/// 执行发送操作并等待返回结果。
	/// </summary>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>包含处理程序返回结果的任务。</returns>
	/// <exception cref="OperationCanceledException">当 <paramref name="cancellationToken"/> 被取消时抛出。</exception>
	/// <exception cref="MessageDeliverException">当传输层完成通知但未提供任何结果时抛出。</exception>
	/// <remarks>
	/// 结果通过 <see cref="Subject{TResult}"/> 回传。仅当传输层既未推送结果也未推送错误、
	/// 而是直接发出完成通知时，以异常结束而非永久等待。
	/// </remarks>
	public async Task<TResult> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
	{
		// 续体不应同步运行在传输层的回调线程上。
		var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

		_subject ??= new Subject<TResult>();
		var subscription = _subject.Subscribe(result => tcs.TrySetResult(result),
		                                       exception => tcs.TrySetException(exception),
		                                       () => tcs.TrySetException(new MessageDeliverException("The send operation completed without producing a result.")));

		try
		{
			await _bus.SendAsync(_message, _subject, Options, Pipeline, cancellationToken);
			return await tcs.Task.WaitAsync(cancellationToken);
		}
		finally
		{
			subscription.Dispose();
			_subject.OnCompleted();
		}
	}
}