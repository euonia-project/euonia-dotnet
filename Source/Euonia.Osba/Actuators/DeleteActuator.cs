namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于删除目标对象的执行器。在保存前将目标对象标记为已删除状态。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
/// <remarks>
/// 在基类执行流程的后续处理阶段，将目标对象标记为已删除（<see cref="ObjectEditState.Deleted"/>），
/// 从而确保在 <see cref="ActuatorBase{TTarget}.ExecuteAsync(CancellationToken)"/> 保存阶段按删除语义持久化。
/// </remarks>
public class DeleteActuator<TTarget> : EditableActuator<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化删除执行器。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	public DeleteActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <summary>
	/// 在主要处理逻辑完成后、保存前执行的后续处理：将目标对象标记为已删除状态。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步操作的 <see cref="Task"/>。</returns>
	/// <remarks>
	/// 无条件调用 <see cref="ObservableObject{T}.MarkAsDeleted"/> 将目标对象的状态标记为
	/// <see cref="ObjectEditState.Deleted"/>，随后委托给基类实现。
	/// </remarks>
	protected override Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		target.MarkAsDeleted();
		return base.ContinueHandleAsync(target, cancellationToken);
	}
}
