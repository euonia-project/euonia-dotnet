namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 消息契约。
/// </summary>
public interface IMessage
{
	/// <summary>
	/// 获取消息标识符。
	/// </summary>
	string MessageId { get; }
}