namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 发送（单播）消息的选项。
/// </summary>
public class SendOptions : ExtendableOptions
{
	/// <summary>
	/// 获取或设置关联标识符。
	/// </summary>
	public string CorrelationId { get; set; }
}