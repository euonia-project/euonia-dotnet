namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 调用（请求-响应）消息的选项。
/// </summary>
public class CallOptions : ExtendableOptions
{
	/// <summary>
	/// 获取或设置关联标识符。
	/// </summary>
	public string CorrelationId { get; set; }
}