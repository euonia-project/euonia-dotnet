using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 定义了可编辑对象的执行器行为接口，继承自通用的管道行为接口 <see cref="IPipelineBehavior{TRequest, TResponse}"/>，用于在处理可编辑对象时实现特定的业务逻辑。
/// </summary>
/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{T}"/>。</typeparam>
public interface IActuatorBehavior<TTarget> : IPipelineBehavior<TTarget, TTarget>
	where TTarget : EditableObject<TTarget>
{
}
