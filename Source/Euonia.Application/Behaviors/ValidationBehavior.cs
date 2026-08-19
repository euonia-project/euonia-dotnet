using Microsoft.Extensions.DependencyInjection;
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
/// 验证器优先从依赖注入容器中解析（<see cref="IValidatorFactory"/>），
/// 未注册时回退到静态 <see cref="ValidatorFactory"/>；若两者都没有配置验证器则跳过验证。
/// 如果验证失败，将在调用下一个管道委托之前抛出异常。
/// </remarks>
public class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private readonly IValidatorFactory _validatorFactory;

	/// <summary>
	/// 初始化 <see cref="ValidationBehavior{TMessage, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="provider">
	/// 服务提供程序，用于可选地解析 <see cref="IValidatorFactory"/>。
	/// 该服务注册为单例，未注册时验证回退到静态 <see cref="ValidatorFactory"/>。
	/// </param>
	public ValidationBehavior(IServiceProvider provider)
	{
		_validatorFactory = provider.GetService<IValidatorFactory>();
	}

	/// <summary>
	/// 通过验证消息数据来处理消息，然后调用管道中的下一个行为。
	/// </summary>
	/// <param name="context">包含待验证数据的消息上下文。</param>
	/// <param name="next">验证成功后要调用的管道中的下一个委托。</param>
	/// <returns>表示异步操作的任务，包含管道的响应。</returns>
	/// <exception cref="ValidationException">当验证失败时抛出。</exception>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		await ValidateAsync(context.Payload);
		return await next(context);
	}

	private async Task ValidateAsync(object payload)
	{
		if (payload is IValidatableObject)
		{
			// 自带验证逻辑的对象复用静态入口（其内部会走 IValidatableObject 分支）。
			await Validator.ValidateAsync(payload);
			return;
		}

		var validator = _validatorFactory?.Create() ?? ValidatorFactory.Create();
		if (validator != null)
		{
			await validator.ValidateAsync(payload);
		}
	}
}
