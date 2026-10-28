namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示被标记的方法将订阅并处理消息。
/// </summary>
/// <remarks>
/// 同一方法只能声明一个 <see cref="SubscribeAttribute"/>；需要多通道别名时请在路由层处理。
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
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