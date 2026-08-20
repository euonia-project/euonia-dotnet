namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于创建目标对象的执行器。在保存前将目标对象标记为新增状态。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
public class CreateActuator<TTarget> : ActuatorBase<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化创建执行器。
	/// </summary>
	/// <param name="builder">包含工作单元配置的构建器实例。</param>
	/// <param name="factory">用于创建目标对象的工厂委托。</param>
	public CreateActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <inheritdoc/>
	protected override Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		target.MarkAsNew();
		return base.ContinueHandleAsync(target, cancellationToken);
	}
}
