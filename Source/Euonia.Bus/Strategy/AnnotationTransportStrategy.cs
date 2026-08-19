using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 通过类型上修饰的特性来评估该类型是否可以被该策略处理。
/// </summary>
public class AnnotationTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// 获取策略的名称。
	/// </summary>
	public string Name { get; } = "Attribute decoration handle strategy";

	/// <summary>
	/// 获取类型要被此策略处理所需的传输名称集合。
	/// </summary>
	private IEnumerable<string> Required { get; }

	/// <summary>
	/// 初始化 <see cref="AnnotationTransportStrategy"/> 类的新实例。
	/// </summary>
	/// <param name="requiredTransports">所需的传输名称集合。</param>
	public AnnotationTransportStrategy(IEnumerable<string> requiredTransports)
	{
		Required = requiredTransports;
	}

	/// <summary>
	/// 判断指定的消息通道是否可以通过此传输策略进行传出（分发）。
	/// 通过检查消息类型是否标记了 <see cref="DispatchInAttribute"/> 特性，且其传输名称与所需传输名称有交集来判断。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果通道允许传出，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Outgoing(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);

		ArgumentNullException.ThrowIfNull(type);

		var attribute = type.GetCustomAttribute<DispatchInAttribute>();

		return attribute != null && Required.Intersect(attribute.Transports).Any();
	}

	/// <summary>
	/// 判断指定的消息通道是否可以通过此传输策略进行传入（接收）。
	/// 通过检查消息类型是否标记了 <see cref="ReceiveInAttribute"/> 特性，且其传输名称与所需传输名称有交集来判断。
	/// </summary>
	/// <param name="channel">要检查的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果通道允许传入，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Incoming(string channel, Type type)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(type);

		var attribute = type.GetCustomAttribute<ReceiveInAttribute>();

		return attribute != null && Required.Intersect(attribute.Transports).Any();
	}
}