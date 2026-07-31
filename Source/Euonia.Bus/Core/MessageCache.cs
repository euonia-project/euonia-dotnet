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
	public string GetOrAddChannel<TMessage>()
	{
		return GetOrAddChannel(typeof(TMessage));
	}

	/// <summary>
	/// Gets message channel name for the specified message type.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	public string GetOrAddChannel(Type messageType)
	{
		return _channels.GetOrAdd(messageType, _ =>
		{
			var channelAttribute = messageType.GetCustomAttribute<ChannelAttribute>();
			if (channelAttribute != null)
			{
				return channelAttribute.Name;
			}

			return messageType.IsSubclassOf(typeof(ITransportable)) ? messageType.FullName : null;
		});
	}
}