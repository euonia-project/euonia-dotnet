namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息分发器接口，用于确定应负责处理消息的传输器列表。
/// </summary>
public interface IDispatcher
{
	/// <summary>
	/// 为指定的通道确定负责分发的传输器列表。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <returns>负责分发该通道消息的传输器名称集合。</returns>
	IEnumerable<string> Determine(string channel);
}