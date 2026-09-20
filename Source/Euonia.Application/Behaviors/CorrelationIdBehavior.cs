using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 负责将当前请求的关联标识符（CorrelationId / RequestTraceId）挂载到消息元数据中，供跨服务透传。
/// </summary>
/// <typeparam name="TMessage">由管道处理的消息类型。必须是实现了 <see cref="IMessageEnvelope"/> 接口的类。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
/// <remarks>
/// 关联查找优先级：请求上下文 <see cref="RequestContext.TraceIdentifier"/>（或 <see cref="RequestContext.RequestId"/> 请求头）
/// 优于已存在于消息元数据 / 信封上的 <see cref="MessageHeaders.RequestTraceId"/> 与 <see cref="MessageHeaders.CorrelationId"/>，
/// 全部缺失时生成新的关联标识符，保证每次处理都能透传一个可关联的标识。
/// </remarks>
public class CorrelationIdBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private readonly IRequestContextAccessor _contextAccessor;

	/// <summary>
	/// 初始化 <see cref="CorrelationIdBehavior{TMessage, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="contextAccessor">用于读取当前请求上下文的访问器。</param>
	public CorrelationIdBehavior(IRequestContextAccessor contextAccessor)
	{
		_contextAccessor = contextAccessor;
	}

	/// <summary>
	/// 处理管道中的消息，将请求上下文或消息自身携带的关联标识写入消息元数据，然后调用下一个管道委托。
	/// </summary>
	/// <param name="context">正在处理的消息信封。</param>
	/// <param name="next">下一个要调用的管道委托。</param>
	/// <returns>包含管道响应结果的任务。</returns>
	public Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		var requestContext = _contextAccessor?.Context;
		var requestTraceId = requestContext?.TraceIdentifier ?? requestContext?.RequestId
		                                                      ?? context.Metadata[MessageHeaders.RequestTraceId] as string;

		if (!string.IsNullOrEmpty(requestTraceId))
		{
			context.Metadata.Set(MessageHeaders.RequestTraceId, requestTraceId);
		}

		var correlationId = requestTraceId
		                    ?? context.Metadata[MessageHeaders.CorrelationId] as string
		                    ?? context.CorrelationId;

		if (string.IsNullOrEmpty(correlationId))
		{
			correlationId = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();
		}

		context.Metadata.Set(MessageHeaders.CorrelationId, correlationId);

		return next(context);
	}
}