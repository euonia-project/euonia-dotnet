namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 定义可编辑对象操作执行器的入口，用于为指定类型创建 <see cref="ActuatorBuilder{TTarget}"/>。
/// </summary>
/// <remarks>
/// 实现类负责提供用于解析 <see cref="ActuatorBuilder{TTarget}"/> 所依赖服务（如对象工厂与执行管道）的服务提供程序。
/// </remarks>
public interface IActuator
{
	/// <summary>
	/// 为指定类型的可编辑对象创建执行器构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	ActuatorBuilder<TTarget> For<TTarget>()
		where TTarget : EditableObject<TTarget>;
}