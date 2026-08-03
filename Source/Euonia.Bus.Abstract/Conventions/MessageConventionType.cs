namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息约定的类型。
/// </summary>
public enum MessageConventionType
{
	/// <summary>
	/// 未分类的消息类型。
	/// </summary>
	None,

	/// <summary>
	/// 单播消息，仅传递给单个接收者。
	/// </summary>
	Unicast,

	/// <summary>
	/// 多播消息，传递给多个接收者。
	/// </summary>
	Multicast,

	/// <summary>
	/// 请求消息，发送给单个接收者并期望收到响应。
	/// </summary>
	Request,
}