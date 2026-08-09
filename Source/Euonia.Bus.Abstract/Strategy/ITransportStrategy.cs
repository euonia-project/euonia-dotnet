namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义传输策略的协定，用于决定消息如何按传出和传入操作进行处理。
/// </summary>
public interface ITransportStrategy
{
	/// <summary>
	/// 获取传输策略的名称。
	/// </summary>
	string Name { get; }

	/// <summary>
	/// 判断指定的通道是否允许用于传出操作。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果通道允许传出，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool Outgoing(string channel, Type type);

	/// <summary>
	/// 判断指定的通道是否允许用于传入操作。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果通道允许传入，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool Incoming(string channel, Type type);
}