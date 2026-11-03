using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器构建器，用于配置执行管道并创建对应操作（获取、创建、删除、执行）的执行器实例。
/// </summary>
/// <typeparam name="TTarget">业务对象的具体类型，必须继承自 <see cref="ObjectBase{TTarget}"/>。</typeparam>
/// <param name="factory">对象工厂，用于获取或创建目标对象。</param>
/// <param name="pipeline">工作单元管理器，用于控制事务边界。</param>
/// <param name="isDefaultPipeline">管道是否由框架默认创建；仅默认且无行为时允许绕过空管道。</param>
/// <remarks>
/// 操作执行器（<see cref="UpdateActuator{TTarget}"/>、<see cref="CreateActuator{TTarget}"/>、<see cref="DeleteActuator{TTarget}"/>、<see cref="ExecuteActuator{TTarget}"/>）
/// 通过 <see cref="ActuatorBuilderExtensions"/> 中的扩展方法创建；扩展方法按目标类型约束区分：
/// 可编辑对象（<see cref="EditableObject{T}"/>）使用 Update/Delete，<see cref="CreateActuator{TTarget}"/> 支持任意业务对象类型，
/// 服务对象（<see cref="ServiceObject{T}"/>）使用 Execute。
/// </remarks>
public sealed class ActuatorBuilder<TTarget>(IObjectFactory factory, IPipeline<TTarget, TTarget> pipeline, bool isDefaultPipeline = false)
	where TTarget : ObjectBase<TTarget>
{
	private int _handlerCount;

	/// <summary>
	/// 获取一个值，指示本次执行是否可以绕过空管道。
	/// </summary>
	/// <remarks>
	/// 仅当管道由 <see cref="Actuator.For{TTarget}(IServiceProvider)"/> 创建、且没有注册任何
	/// <see cref="IActuatorBehavior{TTarget}"/> / <c>Handle</c> / <see cref="Behavior"/> 时成立。
	/// 外部传入的自定义管道始终走完整管道。
	/// </remarks>
	internal bool CanBypassPipeline => isDefaultPipeline && _handlerCount == 0;

	/// <summary>
	/// 添加一个执行器行为。
	/// </summary>
	/// <param name="behavior">执行器行为。</param>
	internal void AddBehavior(IActuatorBehavior<TTarget> behavior)
	{
		pipeline.Use(behavior.HandleAsync);
		_handlerCount++;
	}

	/// <summary>
	/// 添加一个处理程序。
	/// </summary>
	/// <param name="handler">处理程序。</param>
	internal void AddHandler(Func<TTarget, PipelineDelegate<TTarget, TTarget>, Task<TTarget>> handler)
	{
		pipeline.Use(handler);
		_handlerCount++;
	}

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
		// 自定义管道配置可能加入任意组件，保守地视为“不可绕过”。
		_handlerCount++;
		behavior?.Invoke(pipeline);
		return this;
	}
}
