using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息构建器的抽象基类，使用流式 API 配置消息发送选项和管道。
/// </summary>
/// <typeparam name="TBuilder">构建器自身类型，用于链式调用。</typeparam>
/// <typeparam name="TOptions">消息选项的类型，必须继承自 <see cref="ExtendableOptions"/>。</typeparam>
/// <typeparam name="TMessage">消息负载的类型。</typeparam>
/// <typeparam name="TResult">处理结果类型，对于无返回值的操作为 <see cref="Unit"/>。</typeparam>
public abstract class DispatchBuilder<TBuilder, TOptions, TMessage, TResult>
	where TBuilder : DispatchBuilder<TBuilder, TOptions, TMessage, TResult>
	where TOptions : ExtendableOptions
{
	/// <summary>
	/// 使用指定的选项初始化 <see cref="DispatchBuilder{TBuilder,TOptions,TMessage,TResult}"/> 类的新实例。
	/// </summary>
	/// <param name="options">消息发送选项。</param>
	protected DispatchBuilder(TOptions options)
	{
		Options = options;
	}

	/// <summary>
	/// 获取或设置消息处理的管道构建委托。
	/// </summary>
	protected Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> Pipeline { get; set; }

	/// <summary>
	/// 获取消息发送选项。
	/// </summary>
	protected TOptions Options { get; }

	/// <summary>
	/// 设置消息的目标通道。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithChannel(string channel)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		Options.Channel = channel;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置自定义的消息标识符。
	/// </summary>
	/// <param name="messageId">消息标识符。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithMessageId(string messageId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
		Options.MessageId = messageId;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置消息的优先级。
	/// </summary>
	/// <param name="priority">优先级数值。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithPriority(int priority)
	{
		Options.Priority = priority;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置消息处理的超时时间（毫秒）。
	/// </summary>
	/// <param name="timeout">超时时间（毫秒）。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithTimeout(long timeout)
	{
		Options.Timeout = timeout;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置消息处理的超时时间。
	/// </summary>
	/// <param name="timeout">超时时间。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithTimeout(TimeSpan timeout)
	{
		Options.Timeout = (long)timeout.TotalMilliseconds;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置消息处理的延迟时间（毫秒）。
	/// </summary>
	/// <param name="delay">延迟时间（毫秒）。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithDelay(long delay)
	{
		Options.Delay = delay;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置消息元数据的自定义构建委托。
	/// </summary>
	/// <param name="metadataBuilder">用于构建消息元数据的委托。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithMetadata(Action<MessageMetadata> metadataBuilder)
	{
		ArgumentNullException.ThrowIfNull(metadataBuilder);
		Options.MetadataSetter = metadataBuilder;
		return (TBuilder)this;
	}

	/// <summary>
	/// 设置消息处理的管道构建委托。
	/// </summary>
	/// <param name="pipelineBuilder">用于构建消息处理管道的委托。</param>
	/// <returns>返回当前的构建器实例，以便进行链式调用。</returns>
	public TBuilder WithPipeline(Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> pipelineBuilder)
	{
		ArgumentNullException.ThrowIfNull(pipelineBuilder);
		Pipeline = pipelineBuilder;
		return (TBuilder)this;
	}
}