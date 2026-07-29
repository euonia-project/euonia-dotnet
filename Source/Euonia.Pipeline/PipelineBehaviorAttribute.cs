namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// Represents that the class should be handled with pipeline using the specified behavior type. 
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class PipelineBehaviorAttribute : Attribute
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="behaviorType"></param>
	public PipelineBehaviorAttribute(Type behaviorType)
	{
		BehaviorType = behaviorType;
	}

	/// <summary>
	/// Gets the behavior type.
	/// </summary>
	public Type BehaviorType { get; }
}