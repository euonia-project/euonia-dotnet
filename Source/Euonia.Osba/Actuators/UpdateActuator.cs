namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于更新目标对象的执行器。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
/// <remarks>
/// 在基类执行流程的后续处理阶段，若目标对象当前无编辑状态（<see cref="ObjectEditState.None"/>）
/// 但其属性已发生变更，则自动将其标记为已更改（<see cref="ObjectEditState.Changed"/>），
/// 从而确保在 <see cref="ActuatorBase{TTarget}.ExecuteAsync(CancellationToken)"/> 保存阶段能被持久化。
/// </remarks>
public class UpdateActuator<TTarget> : EditableActuator<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化更新执行器。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	public UpdateActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <summary>
	/// 在主要处理逻辑完成后、保存前执行的后续处理：将属性已变更但状态仍为 <see cref="ObjectEditState.None"/> 的目标对象标记为已更改。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步操作的 <see cref="Task"/>。</returns>
	/// <remarks>
	/// 仅当目标对象的 <see cref="ObservableObject{T}.State"/> 为 <see cref="ObjectEditState.None"/> 且
	/// <see cref="BusinessObject.HasChangedProperties"/> 为 <c>true</c> 时，调用 <see cref="ObservableObject{T}.MarkAsChanged"/> 更新其状态；
	/// 否则原样保留对象状态，随后委托给基类实现。
	/// </remarks>
	protected override Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		if (target.State == ObjectEditState.None && target.HasChangedProperties)
		{
			target.MarkAsChanged();
		}

		return base.ContinueHandleAsync(target, cancellationToken);
	}
}
