using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 为每个消息创建 <see cref="IServiceScope"/> 和工作单元的管道行为。
/// </summary>
/// <typeparam name="TMessage">由管道处理的路由消息类型。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
/// <remarks>
/// 每次调用时，该行为将：
/// <list type="bullet">
/// <item>创建作用域化的依赖注入作用域，</item>
/// <item>解析 <see cref="IUnitOfWorkManager"/>，</item>
/// <item>开启一个工作单元（非事务性），</item>
/// <item>调用下一个管道委托，</item>
/// <item>完成工作单元并释放作用域和工作单元。</item>
/// </list>
/// </remarks>
public class UnitOfWorkPipelineBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private readonly IServiceScopeFactory _factory;

	/// <summary>
	/// 初始化 <see cref="UnitOfWorkPipelineBehavior{TMessage, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="factory">用于为每个消息创建作用域化 <see cref="IServiceProvider"/> 的服务作用域工厂。</param>
	public UnitOfWorkPipelineBehavior(IServiceScopeFactory factory)
	{
		_factory = factory;
	}

	/// <summary>
	/// 通过创建作用域和工作单元、调用下一个委托并完成工作单元来处理管道调用。
	/// </summary>
	/// <param name="context">正在处理的路由消息。</param>
	/// <param name="next">下一个要调用的管道委托。</param>
	/// <returns>
	/// 一个 <see cref="Task{TResult}"/>，在管道产生的响应中完成。
	/// 工作单元在任务返回之前完成。
	/// </returns>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		using var scope = _factory.CreateScope();
		var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
		using var uow = manager.Begin(isTransactional: false);
		var response = await next(context);
		await uow.CompleteAsync(CancellationToken.None);
		return response;
	}
}