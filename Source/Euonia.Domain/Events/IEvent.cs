namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 事件接口。
/// </summary>
public interface IEvent : IMessage
{
	/// <summary>
	/// 获取事件标识符。
	/// </summary>
	string EventId { get; }

	/// <summary>
	/// 使用事件标识符覆盖消息标识符。
	/// </summary>
	string IMessage.MessageId => EventId;

	/// <summary>
	/// 获取或设置当前事件的序号。
	/// </summary>
	long Sequence { get; set; }

	/// <summary>
	/// 获取事件意图。
	/// </summary>
	/// <returns>
	/// 事件的意图。
	/// </returns>
	string EventIntent { get; set; }

	/// <summary>
	/// 获取事件发起方的 .NET CLR 类型。
	/// </summary>
	/// <returns>
	/// 事件发起方的 .NET CLR 类型。
	/// </returns>
	string OriginatorType { get; set; }

	/// <summary>
	/// 获取发起方标识符。
	/// </summary>
	/// <returns>
	/// 发起方标识符。
	/// </returns>
	string OriginatorId { get; set; }
}