namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器抽象基类，封装了可编辑对象的获取、处理、保存的通用流程。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
/// <remarks>
/// 派生类通过 <see cref="ActuatorBase{TTarget}(ActuatorBuilder{TTarget}, Func{Task{TTarget}})"/> 构造函数接收
/// 构建器配置与对象工厂委托，并通过 <see cref="HandleAsync(Func{TTarget, Task})"/> 或
/// <see cref="Handle(Action{TTarget})"/> 注册处理逻辑；调用 <see cref="ExecuteAsync(CancellationToken)"/> 触发完整流程。
/// 领域事件的发布逻辑当前已停用（见 <see cref="ExecuteAsync(CancellationToken)"/> 中的注释代码）。
/// </remarks>
public abstract class ActuatorBase<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化执行器基类，保存构建器配置和对象工厂委托。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	protected ActuatorBase(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
	{
		Builder = builder;
		Factory = factory;
	}

	/// <summary>
	/// 获取执行器的构建器配置，其中包含供 <see cref="ExecuteAsync(CancellationToken)"/> 使用的执行管道。
	/// </summary>
	protected ActuatorBuilder<TTarget> Builder { get; }

	/// <summary>
	/// 获取用于异步获取或创建目标对象的工厂委托。
	/// </summary>
	protected Func<Task<TTarget>> Factory { get; }

	/// <summary>
	/// 在主要处理逻辑完成后、保存前执行的后续处理。派生类可重写以添加额外操作。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步操作的 <see cref="Task"/>。</returns>
	protected virtual Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 注册对目标对象的异步处理逻辑，返回当前执行器以支持链式调用。
	/// </summary>
	/// <param name="action">对目标对象执行的异步操作。</param>
	/// <returns>当前 <see cref="ActuatorBase{TTarget}"/> 实例，用于链式调用。</returns>
	/// <remarks>
	/// 注册的处理逻辑将在 <see cref="ExecuteAsync(CancellationToken)"/> 执行流程中、保存目标对象之前被调用。
	/// </remarks>
	public ActuatorBase<TTarget> HandleAsync(Func<TTarget, Task> action)
	{
		Builder.Pipeline.Use(async (target, next) =>
		{
			await action(target);
			return await next(target);
		});
		return this;
	}

	/// <summary>
	/// 注册对目标对象的同步处理逻辑，返回当前执行器以支持链式调用。
	/// </summary>
	/// <param name="action">对目标对象执行的同步操作。</param>
	/// <returns>当前 <see cref="ActuatorBase{TTarget}"/> 实例，用于链式调用。</returns>
	/// <remarks>
	/// 同步逻辑会被包装为异步委托，并在 <see cref="ExecuteAsync(CancellationToken)"/> 执行流程中、保存目标对象之前被调用。
	/// </remarks>
	public ActuatorBase<TTarget> Handle(Action<TTarget> action)
	{
		Builder.Pipeline.Use(async (target, next) =>
		{
			action(target);
			return await next(target);
		});
		return this;
	}

	/// <summary>
	/// 执行完整流程：获取目标对象 → 调用处理程序 → 继续处理 → 保存。
	/// </summary>
	/// <remarks>
	/// 整个流程通过 <see cref="ActuatorBuilder{TTarget}"/> 的执行管道运行；若构建器启用了工作单元，
	/// 处理逻辑将在事务边界内执行，且仅在对象状态发生变更（<see cref="EditableObject{T}.IsChanged"/>）时才保存。
	/// 领域事件的自动发布逻辑当前已注释停用。
	/// </remarks>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>处理完成并保存后的目标对象。</returns>
	public async Task<TTarget> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var target = await Factory();

		return await Builder.Pipeline.RunAsync(target, async result =>
		{
			await ContinueHandleAsync(result, cancellationToken);
			return await result.SaveAsync(result.IsChanged, cancellationToken);
		});
	}
}