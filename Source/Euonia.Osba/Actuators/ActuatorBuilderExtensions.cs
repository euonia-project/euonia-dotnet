namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 为 <see cref="ActuatorBuilder{TTarget}"/> 提供操作执行器创建扩展方法，按目标类型约束区分可编辑对象与命令对象。
/// </summary>
/// <remarks>
/// 可编辑对象（<see cref="EditableObject{T}"/>）使用 <see cref="Update{TTarget}(ActuatorBuilder{TTarget}, object[])"/>、
/// <see cref="Create{TTarget}(ActuatorBuilder{TTarget}, object[])"/>、<see cref="Delete{TTarget}(ActuatorBuilder{TTarget}, object[])"/>；
/// 命令对象（<see cref="CommandObject{T}"/>）使用 <see cref="Execute{TTarget}(ActuatorBuilder{TTarget}, object[])"/>。
/// </remarks>
public static class ActuatorBuilderExtensions
{
	/// <summary>
	/// 创建用于更新目标对象的 <see cref="UpdateActuator{TTarget}"/> 实例。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="builder">执行器构建器。</param>
	/// <param name="criteria">用于获取对象的查询条件。</param>
	/// <returns>更新执行器实例。</returns>
	public static UpdateActuator<TTarget> Update<TTarget>(this ActuatorBuilder<TTarget> builder, params object[] criteria)
		where TTarget : EditableObject<TTarget>
	{
		return new UpdateActuator<TTarget>(builder, () => builder.ObjectFactory.FetchAsync<TTarget>(criteria));
	}

	/// <summary>
	/// 创建用于新建目标对象的 <see cref="CreateActuator{TTarget}"/> 实例。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="builder">执行器构建器。</param>
	/// <param name="criteria">用于创建对象的初始化参数。</param>
	/// <returns>创建执行器实例。</returns>
	public static CreateActuator<TTarget> Create<TTarget>(this ActuatorBuilder<TTarget> builder, params object[] criteria)
		where TTarget : EditableObject<TTarget>
	{
		return new CreateActuator<TTarget>(builder, () => builder.ObjectFactory.CreateAsync<TTarget>(criteria));
	}

	/// <summary>
	/// 创建用于删除目标对象的 <see cref="DeleteActuator{TTarget}"/> 实例。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="builder">执行器构建器。</param>
	/// <param name="criteria">用于定位待删除对象的查询条件。</param>
	/// <returns>删除执行器实例。</returns>
	public static DeleteActuator<TTarget> Delete<TTarget>(this ActuatorBuilder<TTarget> builder, params object[] criteria)
		where TTarget : EditableObject<TTarget>
	{
		return new DeleteActuator<TTarget>(builder, () => builder.ObjectFactory.FetchAsync<TTarget>(criteria));
	}

	/// <summary>
	/// 创建用于执行目标命令对象的 <see cref="ExecuteActuator{TTarget}"/> 实例。
	/// </summary>
	/// <typeparam name="TTarget">命令对象的具体类型，必须继承自 <see cref="CommandObject{TTarget}"/>。</typeparam>
	/// <param name="builder">执行器构建器。</param>
	/// <param name="criteria">用于创建命令对象的初始化参数。</param>
	/// <returns>命令执行器实例。</returns>
	/// <remarks>
	/// 通过对象工厂的 <see cref="IObjectFactory.CreateAsync{TTarget}(object[])"/> 创建命令实例（仅创建，不执行）；
	/// 命令体在 <see cref="ExecuteActuator{TTarget}"/> 的终结阶段执行。
	/// </remarks>
	public static ExecuteActuator<TTarget> Execute<TTarget>(this ActuatorBuilder<TTarget> builder, params object[] criteria)
		where TTarget : CommandObject<TTarget>
	{
		return new ExecuteActuator<TTarget>(builder, () => builder.ObjectFactory.CreateAsync<TTarget>(criteria));
	}
}
