namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于约定消息传输器接口，定义发布、发送、调用等方法。
/// </summary>
public interface ITransporter
{
	/// <summary>
	/// 获取传输器的名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 当消息被分发时触发。
	/// </summary>
	event EventHandler<MessageDeliveredEventArgs> Delivered;

	/// <summary>
	/// 发布（多播）指定的消息。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型。</typeparam>
	/// <param name="message">要发布的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// 发送（单播）指定的消息。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	Task SendAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// 发送指定的消息并期望收到一个响应。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型。</typeparam>
	/// <typeparam name="TResponse">响应的类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// 调用指定的请求消息并返回响应结果。
	/// </summary>
	/// <typeparam name="TRequest">请求消息负载的类型。</typeparam>
	/// <typeparam name="TResponse">响应的类型。</typeparam>
	/// <param name="request">请求消息信封。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>包含响应结果的任务。</returns>
	Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> request, CancellationToken cancellationToken = default)
		where TRequest : class
		where TResponse : class;
}