namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 指示被修饰的消息类型应通过队列进行发送的标记特性。
/// 将此特性应用于类以指定该消息使用指定的队列进行单播投递。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EnqueueAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="EnqueueAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="name">队列名称。</param>
	public EnqueueAttribute(string name)
	{
		Name = name;
	}

	/// <summary>
	/// 获取队列名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 获取或设置消息的优先级。
	/// </summary>
	public int Priority { get; set; }
}