namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息接收方契约，负责接收和确认消息，并暴露订阅名称。
/// </summary>
public interface IRecipient : IDisposable
{
	/// <summary>
	/// 当接收到消息时发生。
	/// </summary>
	event EventHandler<MessageReceivedEventArgs> MessageReceived;

	/// <summary>
	/// 当消息被确认时发生。
	/// </summary>
	event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;

	/// <summary>
	/// 获取订阅者名称。
	/// </summary>
	string Name { get; }
}