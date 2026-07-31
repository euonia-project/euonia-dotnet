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
		where TMessage : class
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
			return channelAttribute != null ? channelAttribute.Name : messageType.FullName;
		});
	}
}