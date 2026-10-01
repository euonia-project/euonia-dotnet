namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于创建目标对象的执行器，支持任意 <see cref="BusinessObject{T}"/> 类型。
/// </summary>
/// <typeparam name="TTarget">业务对象的具体类型，必须继承自 <see cref="BusinessObject{T}"/>。</typeparam>
/// <remarks>
/// <para>
/// 创建流程：通过对象工厂调用目标的创建工厂方法构造实例，随后由执行管道处理
/// （<c>Handle(...)</c> 注册的处理逻辑在终结步骤之前执行）。
/// </para>
/// <para>
/// 后续处理阶段，目标是 <see cref="IEditableObject"/> 时将其标记为新增（<see cref="IEditableObject.MarkAsNew"/>）；
/// 终结阶段，目标同时实现 <see cref="ISavable"/> 与 <see cref="ITrackableObject"/> 时
/// 按 <see cref="ITrackableObject.IsChanged"/> 触发保存（插入语义），
/// 其余类型（只读对象、命令对象等不可持久化对象）构造完成即原样返回、不做落库。
/// </para>
/// </remarks>
public class CreateActuator<TTarget> : ActuatorBase<TTarget>
	where TTarget : BusinessObject<TTarget>
{
	/// <summary>
	/// 初始化创建执行器。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标对象的工厂委托。</param>
	public CreateActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <summary>
	/// 在主要处理逻辑完成后、保存前执行的后续处理：将可编辑对象标记为新增状态。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步操作的 <see cref="Task"/>。</returns>
	/// <remarks>
	/// 仅当目标是 <see cref="IEditableObject"/> 时调用 <see cref="IEditableObject.MarkAsNew"/>，
	/// 将状态标记为 <see cref="ObjectEditState.New"/>；其余类型的业务对象无新增状态，直接继续后续流程。
	/// </remarks>
	protected override Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		if (target is IEditableObject editable)
		{
			editable.MarkAsNew();
		}

		return base.ContinueHandleAsync(target, cancellationToken);
	}

	/// <summary>
	/// 终结创建流程：可编辑对象在此保存（插入语义），其余类型原样返回。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步保存操作的任务，包含处理完成后的目标对象。</returns>
	/// <remarks>
	/// 当目标同时实现 <see cref="ISavable"/> 与 <see cref="ITrackableObject"/> 时，
	/// 按 <see cref="ITrackableObject.IsChanged"/> 触发保存，与 <see cref="EditableActuator{TTarget}"/> 的保存语义一致；
	/// 不可保存类型（只读对象、命令对象等）创建即完成，直接返回目标。
	/// </remarks>
	protected override async Task<TTarget> FinalizeAsync(TTarget target, CancellationToken cancellationToken)
	{
		if (target is ISavable savable && target is ITrackableObject trackable)
		{
			return (TTarget)await savable.SaveAsync(trackable.IsChanged, cancellationToken);
		}

		return target;
	}
}
