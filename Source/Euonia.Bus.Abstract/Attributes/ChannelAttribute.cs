namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示被标记的通道具有指定的名称。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = false)]
public class ChannelAttribute : Attribute
{
	/// <summary>
	/// 获取通道名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化 <see cref="ChannelAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="name">通道名称。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 null 或空白时抛出。</exception>
	public ChannelAttribute(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException(nameof(name));
		}

		Name = name;
	}
}