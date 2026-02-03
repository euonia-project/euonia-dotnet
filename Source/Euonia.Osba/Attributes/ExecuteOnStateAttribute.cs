namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Indicates that the method should be executed based on the object's state.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ExecuteOnStateAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ExecuteOnStateAttribute"/> class.
	/// </summary>
	/// <param name="states">The object states on which the method should be executed.</param>
	public ExecuteOnStateAttribute(params ObjectEditState[] states)
	{
		States = states;
	}

	/// <summary>
	/// Gets the object states on which the method should be executed.
	/// </summary>
	public IEnumerable<ObjectEditState> States { get; }
}