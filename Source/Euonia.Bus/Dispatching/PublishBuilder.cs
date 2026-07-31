namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于流式配置并执行发布（多播）消息操作的构建器。
/// </summary>
/// <typeparam name="TMessage">要发布的消息类型，必须是引用类型。</typeparam>
public class PublishBuilder<TMessage> : DispatchBuilder<PublishBuilder<TMessage>, PublishOptions, TMessage, Unit>
	where TMessage : class
{
	private readonly IBus _bus;
	private readonly TMessage _message;

	/// <summary>
	/// 初始化 <see cref="PublishBuilder{TMessage}"/> 类的新实例。
	/// </summary>
	/// <param name="bus">消息总线实例。</param>
	/// <param name="message">要发布的消息实例。</param>
	/// <param name="options">发布消息的选项。</param>
	public PublishBuilder(IBus bus, TMessage message, PublishOptions options)
		: base(options)
	{
		_bus = bus;
		_message = message;
	}

	/// <summary>
	/// 使用已配置的选项和管道执行发布操作。
	/// </summary>
	/// <param name="cancellationToken">用于取消发布操作的令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	public Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return _bus.PublishAsync(_message, Options, Pipeline, cancellationToken);
	}
}