namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于更新目标对象的执行器。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
public class UpdateActuator<TTarget> : ActuatorBase<TTarget>
	where TTarget : EditableObject<TTarget>
{
	/// <summary>
	/// 初始化更新执行器。
	/// </summary>
	/// <param name="builder">包含工作单元配置的构建器实例。</param>
	/// <param name="factory">用于获取目标对象的工厂委托。</param>
	public UpdateActuator(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)
		: base(builder, factory)
	{
	}

	/// <inheritdoc />
	protected override Task ContinueHandleAsync(TTarget target, CancellationToken cancellationToken = default)
	{
		if (target.State == ObjectEditState.None && target.HasChangedProperties)
		{
			target.MarkAsChanged();
		}

		return base.ContinueHandleAsync(target, cancellationToken);
	}
}
