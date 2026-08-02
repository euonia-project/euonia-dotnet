using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The message cache.
/// </summary>
internal class MessageCache
{
	private static readonly Lazy<MessageCache> _instance = new(() => new MessageCache());

	private readonly ConcurrentDictionary<Type, string> _channels = new();

	public static MessageCache Default => _instance.Value;

	/// <summary>
	/// Gets message channel name for the specified message type.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <returns></returns>
	public string GetOrAddChannel<TMessage>(string name = null)
	{
		return GetOrAddChannel(typeof(TMessage), name);
	}

	/// <summary>
	/// Gets message channel name for the specified message type.
	/// </summary>
	/// <param name="messageType"></param>
	/// <param name="name">The name of the channel.</param>
	/// <returns></returns>
	public string GetOrAddChannel(Type messageType, string name = null)
	{
		return _channels.GetOrAdd(messageType, _ =>
		{
			return PriorityValueFinder.Find<string>(queue =>
			{
				queue.Enqueue(() => name, 1);
				queue.Enqueue(() => messageType.GetCustomAttribute<ChannelAttribute>()?.Name, 2);
				queue.Enqueue(() => messageType.IsSubclassOf(typeof(ITransportable)) ? messageType.FullName : null, 3);
				queue.Enqueue(() =>
				{
					var attributes = messageType.GetCustomAttributes(false);
					return attributes.Any(t => t is TransportableAttribute) ? messageType.FullName : null;
				}, 4);
			}, value => !string.IsNullOrWhiteSpace(value));
		});
	}
}