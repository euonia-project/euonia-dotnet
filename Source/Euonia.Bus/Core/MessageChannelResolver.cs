using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息缓存，用于缓存消息类型与消息通道名称之间的映射。
/// </summary>
internal class MessageChannelResolver
{
	private static readonly Lazy<MessageChannelResolver> _instance = new(() => new MessageChannelResolver());

	private readonly ConcurrentDictionary<Type, Lazy<string>> _channels = new();

	/// <summary>
	/// 获取 <see cref="MessageChannelResolver"/> 的单例实例。
	/// </summary>
	public static MessageChannelResolver Default => _instance.Value;

	/// <summary>
	/// 获取或创建指定消息类型对应的通道名称。
	/// </summary>
	/// <remarks>
	/// 通道名称按优先级从 <see cref="ChannelAttribute"/> 特性或消息类型标记中解析。
	/// </remarks>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <returns>消息类型对应的通道名称。</returns>
	public string GetOrAddChannel<TMessage>()
	{
		return GetOrAddChannel(typeof(TMessage));
	}

	/// <summary>
	/// 获取或创建指定消息类型对应的通道名称。
	/// </summary>
	/// <remarks>
	/// 通道名称按以下优先级解析，首个非空值即作为结果：
	/// <list type="number">
	/// <item><description><see cref="ChannelAttribute"/> 特性中声明的名称。</description></item>
	/// <item><description>实现 <see cref="ITransportable"/> 时使用消息类型的全名。</description></item>
	/// <item><description>标记了 <see cref="TransportableAttribute"/> 特性时使用消息类型的全名。</description></item>
	/// <item><description>消息类型为类且非原始类型且非抽象类型时使用消息类型的全名。</description></item>
	/// </list>
	/// </remarks>
	/// <param name="messageType">消息类型。</param>
	/// <returns>消息类型对应的通道名称。</returns>
	public string GetOrAddChannel(Type messageType)
	{
		var lazyChannel = _channels.GetOrAdd(messageType, static type => new Lazy<string>(
			() => ResolveChannel(type),
			LazyThreadSafetyMode.ExecutionAndPublication));

		try
		{
			var channel = lazyChannel.Value;
			if (string.IsNullOrWhiteSpace(channel))
			{
				_channels.TryRemove(messageType, out _);
			}

			return channel;
		}
		catch
		{
			_channels.TryRemove(messageType, out _);
			throw;
		}
	}

	private static string ResolveChannel(Type messageType)
	{
		return PriorityValueFinder.Find<string>(queue =>
		{
			queue.Enqueue(() => messageType.GetCustomAttribute<ChannelAttribute>()?.Name, 1);
			queue.Enqueue(() => messageType.IsAssignableTo(typeof(ITransportable)) ? messageType.FullName : null, 2);
			queue.Enqueue(() =>
			{
				var attributes = messageType.GetCustomAttributes(false);
				return attributes.Any(t => t is TransportableAttribute) ? messageType.FullName : null;
			}, 3);
			queue.Enqueue(() => messageType.IsClass && !messageType.IsPrimitive && !messageType.IsAbstract ? messageType.FullName : null, 4);
		}, value => !string.IsNullOrWhiteSpace(value));
	}
}