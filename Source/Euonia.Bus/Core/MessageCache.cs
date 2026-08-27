using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息缓存，用于缓存消息类型与消息通道名称之间的映射。
/// </summary>
internal class MessageCache
{
	private static readonly Lazy<MessageCache> _instance = new(() => new MessageCache());

	private readonly ConcurrentDictionary<Type, string> _channels = new();

	/// <summary>
	/// 获取 <see cref="MessageCache"/> 的单例实例。
	/// </summary>
	public static MessageCache Default => _instance.Value;

	/// <summary>
	/// 获取或创建指定消息类型对应的通道名称。
	/// </summary>
	/// <remarks>
	/// 若未显式指定 <paramref name="name"/>，则按优先级从 <see cref="ChannelAttribute"/> 特性或消息类型标记中解析通道名称。
	/// </remarks>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <param name="name">可选的显式通道名称；为 <c>null</c> 或空白时自动解析。</param>
	/// <returns>消息类型对应的通道名称。</returns>
	public string GetOrAddChannel<TMessage>(string name = null)
	{
		return GetOrAddChannel(typeof(TMessage), name);
	}

	/// <summary>
	/// 获取或创建指定消息类型对应的通道名称。
	/// </summary>
	/// <remarks>
	/// 通道名称按以下优先级解析，首个非空值即作为结果：
	/// <list type="number">
	/// <item><description>显式指定的 <paramref name="name"/>。</description></item>
	/// <item><description><see cref="ChannelAttribute"/> 特性中声明的名称。</description></item>
	/// <item><description>实现 <see cref="ITransportable"/> 时使用消息类型的全名。</description></item>
	/// <item><description>标记了 <see cref="TransportableAttribute"/> 特性时使用消息类型的全名。</description></item>
	/// </list>
	/// </remarks>
	/// <param name="messageType">消息类型。</param>
	/// <param name="name">可选的显式通道名称；为 <c>null</c> 或空白时自动解析。</param>
	/// <returns>消息类型对应的通道名称。</returns>
	public string GetOrAddChannel(Type messageType, string name = null)
	{
		return _channels.GetOrAdd(messageType, _ =>
		{
			return PriorityValueFinder.Find<string>(queue =>
			{
				queue.Enqueue(() => name, 1);
				queue.Enqueue(() => messageType.GetCustomAttribute<ChannelAttribute>()?.Name, 2);
				queue.Enqueue(() => messageType.IsAssignableTo(typeof(ITransportable)) ? messageType.FullName : null, 3);
				queue.Enqueue(() =>
				{
					var attributes = messageType.GetCustomAttributes(false);
					return attributes.Any(t => t is TransportableAttribute) ? messageType.FullName : null;
				}, 4);
				queue.Enqueue(() => messageType.IsClass && !messageType.IsPrimitive && !messageType.IsAbstract ? messageType.FullName : null, 5);
			}, value => !string.IsNullOrWhiteSpace(value));
		});
	}
}