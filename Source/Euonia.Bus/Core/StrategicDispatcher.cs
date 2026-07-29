using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The strategic dispatcher.
/// </summary>
internal class StrategicDispatcher : IDispatcher
{
	private readonly ConcurrentDictionary<Type, IReadOnlyList<string>> _transportCache = new();
	private readonly IMessageBusOptions _options;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategicDispatcher"/> class.
	/// </summary>
	/// <param name="options"></param>
	public StrategicDispatcher(IMessageBusOptions options)
	{
		_options = options;
	}

	/// <summary>
	/// Creates the transports for the specified message type.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	/// <exception cref="MessageTypeException"></exception>
	public IEnumerable<string> Determine(Type messageType)
	{
		var transportTypes = _transportCache.GetOrAdd(messageType, _ =>
		{
			var list = new List<string>();
			foreach (var type in _options.StrategyAssignedTypes)
			{
				var strategy = _options.GetStrategy(type);
				if (strategy.Outgoing(messageType))
				{
					list.Add(type);
				}
			}

			return list;
		});

		switch (transportTypes.Count)
		{
			case 0:
				if (string.IsNullOrEmpty(_options.DefaultTransport))
				{
					throw new MessageTypeException("No transport is configured for the message type.");
				}

				transportTypes = new List<string> { _options.DefaultTransport };
				break;

			case > 1 when !_options.Convention.IsMulticastType(messageType):
				throw new MessageTypeException("Multiple transports are configured for a unicast message type.");
		}

		return transportTypes;
	}
}