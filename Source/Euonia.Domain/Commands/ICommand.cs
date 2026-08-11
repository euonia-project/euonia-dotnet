namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 表示命令消息。
/// </summary>
public interface ICommand : IMessage
{
	/// <summary>
	/// 获取命令标识符。
	/// </summary>
	string CommandId { get; }

	/// <summary>
	/// 覆盖消息标识符，使其与命令标识符保持一致。
	/// </summary>
	string IMessage.MessageId => CommandId;
}