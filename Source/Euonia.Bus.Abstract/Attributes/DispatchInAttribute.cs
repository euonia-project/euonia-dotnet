namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 指定消息通过哪些传输器进行分发的特性。
/// 将此特性应用于消息类型以限定该消息只能通过指定的传输器进行发送或发布。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DispatchInAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="DispatchInAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="transports">用于分发消息的传输器名称数组。</param>
	public DispatchInAttribute(params string[] transports)
	{
		Transports = transports;
	}

	/// <summary>
	/// 获取用于分发消息的传输器名称集合。
	/// </summary>
	public IEnumerable<string> Transports { get; }
}