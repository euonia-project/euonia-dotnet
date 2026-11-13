using System.Diagnostics.CodeAnalysis;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 为命令处理程序提供基础实现，包含常用的异步执行辅助方法。
/// </summary>
/// <remarks>
/// 本类为子类提供 <see cref="Factory"/> 与 <see cref="Actuator"/> 依赖，
/// 并提供 <see cref="ExecuteAsync(Func{Task}, CancellationToken)"/> 与
/// <see cref="ExecuteAsync{TResult}(Func{Task{TResult}}, Action{TResult}, CancellationToken)"/>
/// 辅助方法，用于统一命令操作的等待方式与结果传递方式。
/// </remarks>
public abstract class CommandHandlerBase
{
	/// <summary>
	/// 获取用于创建和提取业务对象的对象工厂。
	/// </summary>
	protected IObjectFactory Factory { get; }

	/// <summary>
	/// 获取用于为可编辑对象构建操作（例如创建、更新、删除）的执行器。
	/// </summary>
	protected IActuator Actuator { get; }

	/// <summary>
	/// 初始化 <see cref="CommandHandlerBase"/> 类的新实例。
	/// </summary>
	/// <param name="factory">用于业务对象操作的对象工厂。</param>
	/// <param name="actuator">用于构建可编辑对象操作的执行器。</param>
	protected CommandHandlerBase(IObjectFactory factory, IActuator actuator)
	{
		Factory = factory;
		Actuator = actuator;
	}

	/// <summary>
	/// 异步执行指定的操作。
	/// </summary>
	/// <param name="action">要执行的异步操作。此参数不能为 <see langword="null"/>。</param>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步执行的 <see cref="Task"/>。</returns>
	/// <remarks>
	/// 该操作会被等待，其抛出的任何异常都会传播给调用方。
	/// 此重载不产生结果；若需要把结果传递给后续操作，
	/// 请使用 <see cref="ExecuteAsync{TResult}(Func{Task{TResult}}, Action{TResult}, CancellationToken)"/>。
	/// </remarks>
	protected virtual async Task ExecuteAsync([NotNull] Func<Task> action, CancellationToken cancellationToken = default)
	{
		await action();
	}

	/// <summary>
	/// 异步执行一个产生结果的操作，然后以该结果调用同步的后续操作。
	/// </summary>
	/// <typeparam name="TResult">操作产生的结果类型。</typeparam>
	/// <param name="action">产生结果的异步操作。此参数不能为 <see langword="null"/>。</param>
	/// <param name="next">以所产生结果调用的同步后续操作。此参数不能为 <see langword="null"/>。</param>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步执行的 <see cref="Task"/>。</returns>
	/// <remarks>
	/// 该操作会被等待，其结果会传递给 <paramref name="next"/>。
	/// 操作或后续操作抛出的异常都会传播给调用方。
	/// </remarks>
	protected virtual async Task ExecuteAsync<TResult>([NotNull] Func<Task<TResult>> action, Action<TResult> next, CancellationToken cancellationToken = default)
	{
		var result = await action();
		next(result);
	}
}