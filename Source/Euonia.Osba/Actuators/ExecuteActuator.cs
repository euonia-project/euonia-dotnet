namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于执行目标服务对象的执行器。
/// </summary>
/// <typeparam name="TTarget">服务对象的具体类型，必须继承自 <see cref="ServiceObject{T}"/>。</typeparam>
/// <remarks>
/// 在基类执行流程的终结阶段，通过构建器关联的对象工厂调用
/// <see cref="IObjectFactory.ExecuteAsync{TTarget}(TTarget, CancellationToken)"/>，
/// 将操作分派到目标服务对象上以 <see cref="FactoryExecuteAttribute"/> 标记的方法（或按约定命名的 Execute/ExecuteAsync 方法）。
/// </remarks>
public class ExecuteActuator<TTarget> : ActuatorBase<TTarget>
	where TTarget : ServiceObject<TTarget>
{
	/// <summary>
	/// 初始化操作执行器。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取或创建目标服务对象的工厂委托。</param>
	public ExecuteActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <summary>
	/// 执行执行体：调用 <see cref="IObjectFactory.ExecuteAsync{TTarget}(TTarget, CancellationToken)"/> 分派命令。
	/// </summary>
	/// <param name="target">已处理的目标服务对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步执行操作的任务，包含执行后的服务对象。</returns>
	protected override Task<TTarget> FinalizeAsync(TTarget target, CancellationToken cancellationToken)
	{
		return Builder.ObjectFactory.ExecuteAsync(target, cancellationToken);
	}
}
