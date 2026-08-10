namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 指示应基于对象状态执行的方法。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ExecuteOnStateAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="ExecuteOnStateAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="states">应执行该方法的对象状态。</param>
	public ExecuteOnStateAttribute(params ObjectEditState[] states)
	{
		States = states;
	}

	/// <summary>
	/// 获取应执行该方法的对象状态。
	/// </summary>
	public IEnumerable<ObjectEditState> States { get; }
}