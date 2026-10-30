namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于读取目标对象的执行器。
/// </summary>
/// <typeparam name="TTarget">业务对象的具体类型，必须继承自 <see cref="ObjectBase{T}"/>。</typeparam>
/// <remarks>
/// 读取流程：通过对象工厂调用目标的读取工厂方法（<see cref="FactoryFetchAttribute"/> 或约定名），
/// 随后由执行管道处理（<c>Handle(...)</c> 注册的逻辑在终结步骤之前执行）。
/// 读取本身不触发保存；<c>Handle</c> 中的修改不会落库，权限与数据范围由工厂边界负责。
/// </remarks>
public class FetchActuator<TTarget> : ActuatorBase<TTarget>
	where TTarget : ObjectBase<TTarget>
{
	/// <summary>
	/// 初始化读取执行器。
	/// </summary>
	/// <param name="builder">包含执行管道配置的构建器实例。</param>
	/// <param name="factory">用于异步获取目标对象的工厂委托。</param>
	public FetchActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <summary>
	/// 终结读取流程：目标对象原样返回，不做保存。
	/// </summary>
	/// <param name="target">已处理的目标对象。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>包含目标对象的任务。</returns>
	protected override Task<TTarget> FinalizeAsync(TTarget target, CancellationToken cancellationToken)
	{
		return Task.FromResult(target);
	}
}
