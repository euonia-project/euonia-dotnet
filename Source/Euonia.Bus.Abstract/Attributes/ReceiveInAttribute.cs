namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示一个特性，用于指定消息在哪些传输通道中接收。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ReceiveInAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="ReceiveInAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="transports">接收消息的传输通道名称。</param>
	public ReceiveInAttribute(params string[] transports)
	{
		Transports = transports;
	}

	/// <summary>
	/// 获取接收消息的传输通道名称。
	/// </summary>
	public IEnumerable<string> Transports { get; }
}