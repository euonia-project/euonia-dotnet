using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器构建器，用于配置工作单元选项并创建对应操作（获取、创建、删除）的执行器实例。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
/// <param name="factory">对象工厂，用于获取或创建目标对象。</param>
/// <param name="pipeline">工作单元管理器，用于控制事务边界。</param>
public sealed class ActuatorBuilder<TTarget>(IObjectFactory factory, IPipeline<TTarget, TTarget> pipeline)
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 获取关联的工作单元管理器。
	/// </summary>
	internal IPipeline<TTarget, TTarget> Pipeline => pipeline;

	/// <summary>
	/// 配置管道
	/// </summary>
	/// <param name="behavior">用于配置管道的操作。</param>
	/// <returns>当前 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	public ActuatorBuilder<TTarget> Behavior(Action<IPipeline<TTarget, TTarget>> behavior)
	{
		behavior?.Invoke(pipeline);
		return this;
	}

	/// <summary>
	/// 创建用于更新目标对象的 <see cref="UpdateActuator{TTarget}"/> 实例。
	/// </summary>
	/// <param name="criteria">用于获取对象的查询条件。</param>
	/// <returns>更新执行器实例。</returns>
	public UpdateActuator<TTarget> Update(params object[] criteria)
	{
		return new UpdateActuator<TTarget>(this, () => factory.FetchAsync<TTarget>(criteria));
	}

	/// <summary>
	/// 创建用于新建目标对象的 <see cref="CreateActuator{TTarget}"/> 实例。
	/// </summary>
	/// <param name="criteria">用于创建对象的初始化参数。</param>
	/// <returns>创建执行器实例。</returns>
	public CreateActuator<TTarget> Create(params object[] criteria)
	{
		return new CreateActuator<TTarget>(this, () => factory.CreateAsync<TTarget>(criteria));
	}

	/// <summary>
	/// 创建用于删除目标对象的 <see cref="DeleteActuator{TTarget}"/> 实例。
	/// </summary>
	/// <param name="criteria">用于定位待删除对象的查询条件。</param>
	/// <returns>删除执行器实例。</returns>
	public DeleteActuator<TTarget> Delete(params object[] criteria)
	{
		return new DeleteActuator<TTarget>(this, () => factory.FetchAsync<TTarget>(criteria));
	}
}
