namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示 <see cref="ITransportStrategy"/> 接口的默认实现。
/// 此策略不允许任何消息类型进行传出或传入操作。
/// </summary>
internal class DefaultTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// 获取传输策略的名称。
	/// </summary>
	public string Name { get; } = "Default Transport Strategy";

	/// <summary>
	/// 判断指定的消息通道是否允许用于传出操作。
	/// 此实现始终返回 <c>false</c>。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <returns>始终返回 <c>false</c>，表示不允许传出操作。</returns>
	public bool Outgoing(string channel)
	{
		return false;
	}

	/// <summary>
	/// 判断指定的消息通道是否允许用于传入操作。
	/// 此实现始终返回 <c>false</c>。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <returns>始终返回 <c>false</c>，表示不允许传入操作。</returns>
	public bool Incoming(string channel)
	{
		return false;
	}
}