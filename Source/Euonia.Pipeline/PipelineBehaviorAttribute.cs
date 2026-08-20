namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 表示带有此特性的类应使用指定的行为类型通过管道进行处理。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class PipelineBehaviorAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="PipelineBehaviorAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="behaviorType">用于处理该类的管道行为类型。</param>
	/// <param name="priority">行为的执行优先级，数字越小越先执行；未指定时按注册顺序推导。</param>
	public PipelineBehaviorAttribute(Type behaviorType, int priority = 0)
	{
		BehaviorType = behaviorType;
		Priority = priority;
	}

	/// <summary>
	/// 获取用于处理该类的管道行为类型。
	/// </summary>
	public Type BehaviorType { get; }

	/// <summary>
	/// 获取行为的执行优先级，数字越小越先执行。
	/// </summary>
	public int Priority { get; }
}
