using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Sample.Facade.Behaviors;

/// <summary>
/// 请求信息管道行为：预留给总线管道采集请求上下文信息，当前不做处理而直接调用后续行为。
/// </summary>
/// <typeparam name="TMessage">管道处理的消息信封类型。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
internal class RequestInfoBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	/// <inheritdoc/>
	public Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		return next(context);
	}
}