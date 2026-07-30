namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于流式配置并执行调用（请求-响应）消息操作的构建器。
/// </summary>
/// <typeparam name="TMessage">请求消息的类型。</typeparam>
/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
public sealed class CallBuilder<TMessage, TResult> : DispatchBuilder<CallBuilder<TMessage, TResult>, CallOptions, TMessage, TResult>
{
	private readonly IBus _bus;
	private readonly TMessage _message;

	/// <summary>
	/// 初始化 <see cref="CallBuilder{TMessage, TResult}"/> 类的新实例。
	/// </summary>
	/// <param name="bus">消息总线实例。</param>
	/// <param name="message">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="options">调用消息的选项。</param>
	public CallBuilder(IBus bus, TMessage message, CallOptions options)
		: base(options)
	{
		_bus = bus;
		_message = message;
	}

	/// <summary>
	/// 设置关联标识符。
	/// </summary>
	/// <param name="correlationId">关联标识符。</param>
	/// <returns>返回当前的 <see cref="CallBuilder{TMessage, TResult}"/> 实例，以便进行链式调用。</returns>
	public CallBuilder<TMessage, TResult> WithCorrelationId(string correlationId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
		Options.CorrelationId = correlationId;
		return this;
	}

	/// <summary>
	/// 使用已配置的选项和管道执行调用操作，并返回结果。
	/// </summary>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>包含请求处理程序返回结果的任务。</returns>
	public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return _bus.CallAsync(_message, Options, Pipeline, cancellationToken);
	}
}