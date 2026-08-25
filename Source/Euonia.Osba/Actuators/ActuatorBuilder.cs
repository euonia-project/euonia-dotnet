using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器构建器，用于配置执行管道并创建对应操作（获取、创建、删除、执行）的执行器实例。
/// </summary>
/// <typeparam name="TTarget">业务对象的具体类型，必须继承自 <see cref="BusinessObject{TTarget}"/>。</typeparam>
/// <param name="factory">对象工厂，用于获取或创建目标对象。</param>
/// <param name="pipeline">工作单元管理器，用于控制事务边界。</param>
/// <remarks>
/// 操作执行器（<see cref="UpdateActuator{TTarget}"/>、<see cref="CreateActuator{TTarget}"/>、<see cref="DeleteActuator{TTarget}"/>、<see cref="ExecuteActuator{TTarget}"/>）
/// 通过 <see cref="ActuatorBuilderExtensions"/> 中的扩展方法创建；扩展方法按目标类型约束区分：
/// 可编辑对象（<see cref="EditableObject{T}"/>）使用 Update/Create/Delete，命令对象（<see cref="CommandObject{T}"/>）使用 Execute。
/// </remarks>
public sealed class ActuatorBuilder<TTarget>(IObjectFactory factory, IPipeline<TTarget, TTarget> pipeline)
	where TTarget : BusinessObject<TTarget>
{
	/// <summary>
	/// 获取关联的工作单元管理器。
	/// </summary>
	internal IPipeline<TTarget, TTarget> Pipeline => pipeline;

	/// <summary>
	/// 获取关联的对象工厂。
	/// </summary>
	internal IObjectFactory ObjectFactory => factory;

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
}
