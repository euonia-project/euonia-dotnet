namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 可编辑对象执行器的抽象基类，实现 <see cref="ActuatorBase{TTarget}"/> 的保存终结步骤。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
/// <remarks>
/// 终结步骤 <see cref="ActuatorBase{TTarget}.FinalizeAsync(TTarget, CancellationToken)"/> 调用目标对象的
/// <see cref="EditableObject{T}.SaveAsync(bool, CancellationToken)"/>；当对象状态为 <see cref="ObjectEditState.None"/>
/// 且未发生变更时，保存操作不执行任何持久化。
/// </remarks>
public abstract class EditableActuator<TTarget> : ActuatorBase<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化可编辑对象执行器基类，保存构建器配置和对象工厂委托。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	protected EditableActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <summary>
	/// 保存目标对象：当对象状态为 <see cref="ObjectEditState.None"/> 且已发生变更（<see cref="ObservableObject{T}.IsChanged"/>）时强制更新。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步保存操作的任务，包含保存后的目标对象。</returns>
	protected override Task<TTarget> FinalizeAsync(TTarget target, CancellationToken cancellationToken)
	{
		return target.SaveAsync(target.IsChanged, cancellationToken);
	}
}
