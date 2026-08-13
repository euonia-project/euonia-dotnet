using System.Reactive.Subjects;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义核心消息总线接口，用于发布、发送和调用消息。
/// </summary>
/// <remarks>
/// 此接口为不同的消息模式提供异步方法：
/// - Publish（发布）：即发即忘的消息模式，将消息发送给所有订阅者
/// - Send（发送）：点对点消息模式，可选响应
/// - Call（调用）：请求-响应模式，用于获取返回结果
/// </remarks>
public interface IBus
{
	/// <summary>
	/// 创建一个用于流式配置发布消息的 <see cref="PublishBuilder{TMessage}"/> 构建器。
	/// </summary>
	/// <typeparam name="TMessage">要发布的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发布的消息实例。</param>
	/// <returns>用于流式配置发布操作的 <see cref="PublishBuilder{TMessage}"/> 实例。</returns>
	PublishBuilder<TMessage> Publish<TMessage>(TMessage message)
		where TMessage : class
	{
		return new PublishBuilder<TMessage>(this, message, new PublishOptions());
	}

	/// <summary>
	/// 创建一个用于流式配置发送消息并接收响应的 <see cref="SendBuilder{TMessage, TResult}"/> 构建器。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResult">期望从处理程序返回的结果类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <returns>用于流式配置发送操作的 <see cref="SendBuilder{TMessage, TResult}"/> 实例。</returns>
	SendBuilder<TMessage, TResult> Send<TMessage, TResult>(TMessage message)
		where TMessage : class
	{
		return new SendBuilder<TMessage, TResult>(this, message, new SendOptions());
	}

	/// <summary>
	/// 创建一个用于流式配置发送消息（无响应）的 <see cref="SendBuilder{TMessage, Unit}"/> 构建器。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <returns>用于流式配置发送操作的 <see cref="SendBuilder{TMessage, Unit}"/> 实例。</returns>
	SendBuilder<TMessage, Unit> Send<TMessage>(TMessage message)
		where TMessage : class
	{
		return Send<TMessage, Unit>(message);
	}

	/// <summary>
	/// 创建一个用于流式配置调用请求并返回结果的 <see cref="CallBuilder{TMessage, TResult}"/> 构建器。
	/// </summary>
	/// <typeparam name="TMessage">请求消息的类型。</typeparam>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="message">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <returns>用于流式配置调用操作的 <see cref="CallBuilder{TMessage, TResult}"/> 实例。</returns>
	CallBuilder<TMessage, TResult> Call<TMessage, TResult>(TMessage message)
	{
		return new CallBuilder<TMessage, TResult>(this, message, new CallOptions());
	}

	/// <summary>
	/// 使用默认选项将消息发布给所有订阅者。
	/// </summary>
	/// <typeparam name="TMessage">要发布的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发布的消息实例。</param>
	/// <param name="cancellationToken">用于取消发布操作的令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
	{
		return PublishAsync(message, new PublishOptions(), behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用默认选项将消息发布给所有订阅者，并允许配置管道行为。
	/// </summary>
	/// <typeparam name="TMessage">要发布的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发布的消息实例。</param>
	/// <param name="options">用于控制发布行为的选项。</param>
	/// <param name="cancellationToken">用于取消发布操作的令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
	{
		return PublishAsync(message, options, behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项和管道行为将消息发布给所有订阅者。
	/// </summary>
	/// <typeparam name="TMessage">要发布的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发布的消息实例。</param>
	/// <param name="options">用于控制发布行为的选项。</param>
	/// <param name="behavior">用于在发布前配置管道消息的委托。</param>
	/// <param name="cancellationToken">用于取消发布操作的令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	Task PublishAsync<TMessage>(TMessage message, PublishOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, Unit>> behavior, CancellationToken cancellationToken = default);

	/// <summary>
	/// 使用默认选项将消息发送给单个处理程序，不期望响应。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
	{
		return SendAsync(message, null, null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项和管道行为将消息发送给单个处理程序，不期望响应。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="options">用于控制发送行为的选项。</param>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	Task SendAsync<TMessage>(TMessage message, SendOptions options, CancellationToken cancellationToken = default)
	{
		return SendAsync(message, options, behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项和管道行为将消息发送给单个处理程序，不期望响应。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="behavior">用于在发送前配置管道消息的委托。</param>
	/// <param name="options">用于控制发送行为的选项。</param>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	Task SendAsync<TMessage>(TMessage message, SendOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, Unit>> behavior, CancellationToken cancellationToken = default)
	{
		return SendAsync(message, null, options, behavior, cancellationToken);
	}

	/// <summary>
	/// 使用默认选项将消息发送给单个处理程序，并通过回调处理响应结果。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResult">期望从处理程序返回的结果类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="callback">用于处理从处理程序接收到的结果的 Subject 对象。</param>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, CancellationToken cancellationToken = default)
	{
		return SendAsync(message, callback, null, null, cancellationToken);
	}

	/// <summary>
	/// 使用默认选项将消息发送给单个处理程序，并通过回调处理响应结果。
	/// </summary>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="callback">用于处理从处理程序接收到的结果的 Subject 对象。</param>
	/// <param name="options">用于控制发送行为的选项。</param>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResult">期望从处理程序返回的结果类型。</typeparam>
	/// <returns>表示异步发送操作的任务。</returns>
	Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, SendOptions options, CancellationToken cancellationToken = default)
	{
		return SendAsync(message, callback, options, behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项和管道行为将消息发送给单个处理程序，并通过回调处理响应结果。
	/// </summary>
	/// <typeparam name="TMessage">要发送的消息类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResult">期望从处理程序返回的结果类型。</typeparam>
	/// <param name="message">要发送的消息实例。</param>
	/// <param name="callback">用于处理从处理程序接收到的结果的 Subject 对象。</param>
	/// <param name="options">用于控制发送行为的选项。</param>
	/// <param name="behavior">用于在发送前配置管道消息的委托。</param>
	/// <param name="cancellationToken">用于取消发送操作的令牌。</param>
	/// <returns>表示异步发送操作的任务。</returns>
	Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, SendOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, CancellationToken cancellationToken = default);

	/// <summary>
	/// 使用默认选项调用请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <typeparam name="TMessage">请求消息的类型。</typeparam>
	/// <param name="message">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TMessage, TResult>(TMessage message, CancellationToken cancellationToken = default)
	{
		return CallAsync<TMessage, TResult>(message, new CallOptions(), behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项调用请求处理程序并返回结果，支持管道行为配置。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <typeparam name="TMessage">请求消息的类型。</typeparam>
	/// <param name="message">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="options">用于控制调用行为的选项。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TMessage, TResult>(TMessage message, CallOptions options, CancellationToken cancellationToken = default)
	{
		return CallAsync<TMessage, TResult>(message, options, behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项和管道行为调用请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <typeparam name="TMessage">请求消息的类型。</typeparam>
	/// <param name="message">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="options">用于控制调用行为的选项。</param>
	/// <param name="behavior">用于在调用前配置管道消息的委托。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TMessage, TResult>(TMessage message, CallOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, CancellationToken cancellationToken = default);

	/// <summary>
	/// 使用默认选项调用实现了 <see cref="IRequest{TResult}"/> 的请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="request">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
	{
		return CallAsync(request, new CallOptions(), behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项调用实现了 <see cref="IRequest{TResult}"/> 的请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="request">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="options">用于控制调用行为的选项。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TResult>(IRequest<TResult> request, CallOptions options, CancellationToken cancellationToken = default)
	{
		return CallAsync(request, options, behavior: null, cancellationToken);
	}

	/// <summary>
	/// 使用指定的选项和管道行为调用实现了 <see cref="IRequest{TResult}"/> 的请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="request">实现了 <see cref="IRequest{TResult}"/> 的请求消息。</param>
	/// <param name="options">用于控制调用行为的选项。</param>
	/// <param name="behavior">用于在调用前配置管道消息的委托。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TResult>(IRequest<TResult> request, CallOptions options, Action<IPipeline<IMessageEnvelope<IRequest<TResult>>, TResult>> behavior, CancellationToken cancellationToken = default);

	/// <summary>
	/// 使用指定的处理程序调用请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="handler">用于处理请求的委托。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TResult>(Func<IServiceProvider, CancellationToken, Task<TResult>> handler, CancellationToken cancellationToken = default);

	/// <summary>
	/// 使用指定的处理程序调用请求处理程序并返回结果。
	/// </summary>
	/// <typeparam name="TResult">期望从请求处理程序返回的结果类型。</typeparam>
	/// <param name="handler">用于处理请求的委托。</param>
	/// <param name="cancellationToken">用于取消调用操作的令牌。</param>
	/// <returns>表示异步调用操作的任务，包含返回的结果。</returns>
	Task<TResult> CallAsync<TResult>(Func<CancellationToken, Task<TResult>> handler, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(handler);
		return handler(cancellationToken);
	}
}