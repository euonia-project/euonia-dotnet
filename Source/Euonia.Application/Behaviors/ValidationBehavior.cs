using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 在消息处理前验证消息数据的管道行为。
/// </summary>
/// <typeparam name="TMessage">正在处理的消息类型，必须是实现了 <see cref="IMessageEnvelope"/> 接口的类。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
/// <remarks>
/// 该行为在管道中拦截消息，并使用配置的 <see cref="IValidator"/> 验证其数据。
/// 如果验证失败，将在调用下一个管道委托之前抛出异常。
/// </remarks>
public class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	/// <summary>
	/// 通过验证消息数据来处理消息，然后调用管道中的下一个行为。
	/// </summary>
	/// <param name="context">包含待验证数据的消息上下文。</param>
	/// <param name="next">验证成功后要调用的管道中的下一个委托。</param>
	/// <returns>表示异步操作的任务，包含管道的响应。</returns>
	/// <exception cref="ValidationException">当验证失败时抛出。</exception>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		await Validator.ValidateAsync(context.Payload);
		return await next(context);
	}
}