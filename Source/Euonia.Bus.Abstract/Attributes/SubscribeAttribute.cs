namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示被标记的方法将订阅并处理消息。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class SubscribeAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="SubscribeAttribute"/> 类的新实例。
	/// </summary>
	public SubscribeAttribute()
	{
	}

	/// <summary>
	/// 使用指定的消息名称初始化 <see cref="SubscribeAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="name">消息名称。</param>
	public SubscribeAttribute(string name)
		: this()
	{
		Name = name;
	}

	/// <summary>
	/// 获取消息名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 获取或设置消息分组名称。
	/// </summary>
	public string Group { get; set; }
}