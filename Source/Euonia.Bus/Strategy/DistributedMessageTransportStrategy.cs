using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示一个传输策略，用于评估类型是否标记为分布式消息。
/// </summary>
public class DistributedMessageTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// 获取传输策略的名称。
	/// </summary>
	public string Name => "Distributed message transport strategy";

	/// <summary>
	/// 判断指定的消息通道是否允许用于传出操作。
	/// 通过检查消息类型是否标记了 <see cref="DistributedMessageAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">要评估的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果消息类型标记了 <see cref="DistributedMessageAttribute"/>，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Outgoing(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		type ??= DefaultConfigurator.Instance.Registrations.TryGetValue(channel)?.MessageType;
		return type?.GetCustomAttribute<DistributedMessageAttribute>() != null;
	}

	/// <summary>
	/// 判断指定的消息通道是否允许用于传入操作。
	/// 通过检查消息类型是否标记了 <see cref="DistributedMessageAttribute"/> 特性来判断。
	/// </summary>
	/// <param name="channel">要评估的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果消息类型标记了 <see cref="DistributedMessageAttribute"/>，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Incoming(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		type ??= DefaultConfigurator.Instance.Registrations.TryGetValue(channel)?.MessageType;
		return type?.GetCustomAttribute<DistributedMessageAttribute>() != null;
	}
}