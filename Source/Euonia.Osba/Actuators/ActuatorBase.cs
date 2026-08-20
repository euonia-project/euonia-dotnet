namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器抽象基类，封装了可编辑对象的获取、处理、保存和领域事件发布的通用流程。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
public abstract class ActuatorBase<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化执行器基类，保存构建器配置和对象工厂委托。
	/// </summary>
	/// <param name="builder">包含工作单元配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	protected ActuatorBase(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
	{
		Builder = builder;
		Factory = factory;
	}

	private ActuatorBuilder<TTarget> Builder { get; set; }

	/// <summary>
	/// 获取用于异步获取或创建目标对象的工厂委托。
	/// </summary>
	protected Func<Task<TTarget>> Factory { get; set; }

	/// <summary>
	/// 获取或设置对目标对象执行的处理委托。
	/// </summary>
	protected Func<TTarget, Task> Handler { get; set; }

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
	/// <returns>当前 <see cref="ActuatorBase{TTarget}"/> 实例。</returns>
	public virtual ActuatorBase<TTarget> HandleAsync(Func<TTarget, Task> action)
	{
		Handler = action;
		return this;
	}

	/// <summary>
	/// 注册对目标对象的同步处理逻辑，返回当前执行器以支持链式调用。
	/// </summary>
	/// <param name="action">对目标对象执行的同步操作。</param>
	/// <returns>当前 <see cref="ActuatorBase{TTarget}"/> 实例。</returns>
	public virtual ActuatorBase<TTarget> Handle(Action<TTarget> action)
	{
		Handler = target =>
		{
			action(target);
			return Task.CompletedTask;
		};
		return this;
	}

	/// <summary>
	/// 执行完整流程：获取目标对象 → 调用处理程序 → 继续处理 → 保存 → 发布领域事件。
	/// </summary>
	/// <remarks>
	/// 如果构建器启用了工作单元，处理逻辑将在事务边界内执行。
	/// 处理完成后会检查目标对象是否实现了 <see cref="IHasDomainEvents"/>，若有则自动发布领域事件。
	/// </remarks>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>处理完成并保存后的目标对象。</returns>
	public async Task<TTarget> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var target = await Factory();

		return await Builder.Pipeline.RunAsync(target, async (result) =>
		{
			if (Handler != null)
			{
				await Handler(result);
			}

			await ContinueHandleAsync(result, cancellationToken);
			return await result.SaveAsync(result.IsChanged, cancellationToken);
		});

		//await PublishEventsAsync();

		//async Task PublishEventsAsync()
		//{
		//	if (target is not IHasDomainEvents domain)
		//	{
		//		return;
		//	}

		//	var events = domain.GetEvents();
		//	if (events.Count < 1)
		//	{
		//		return;
		//	}

		//	var bus = target.BusinessContext.GetService<IBus>();
		//	var request = target.BusinessContext.GetService<IRequestContextAccessor>();
		//	var options = new PublishOptions
		//	{
		//		RequestTraceId = request?.Context?.TraceIdentifier
		//	};
		//	foreach (var @event in events)
		//	{
		//		await bus.PublishAsync(@event, options, null, cancellationToken);
		//	}
		//}
	}
}