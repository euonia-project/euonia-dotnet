using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 策略化消息分发器，根据传输策略决定消息应由哪些传输器分发，并对结果进行缓存。
/// </summary>
internal class StrategicDispatcher : IDispatcher
{
	private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _transportCache = new();
	private readonly IConfigurator _configurator;
	private readonly MessageBusOptions _options;

	/// <summary>
	/// 初始化 <see cref="StrategicDispatcher"/> 类的新实例。
	/// </summary>
	/// <param name="configurator">消息总线配置选项。</param>
	/// <param name="options">消息总线配置选项。</param>
	public StrategicDispatcher(IConfigurator configurator, IOptions<MessageBusOptions> options)
	{
		_configurator = configurator;
		_options = options.Value;
	}

	/// <summary>
	/// 为指定的通道确定负责分发的传输器列表。
	/// 遍历所有已分配策略的传输类型，筛选出允许该通道传出的传输器，并对结果进行缓存。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="type">消息类型。</param>
	/// <returns>负责分发该通道消息的传输器名称集合。</returns>
	/// <exception cref="MessageTypeException">
	/// 当无任何传输器匹配且未配置默认传输器时抛出；
	/// 或当多个传输器匹配单播消息类型时抛出。
	/// </exception>
	public IEnumerable<string> Determine(string channel, Type type)
	{
		var transportTypes = _transportCache.GetOrAdd(channel, _ =>
		{
			var list = new List<string>();
			foreach (var transport in _configurator.StrategyAssignedTypes)
			{
				var strategy = _configurator.GetStrategy(transport);
				if (strategy.Outgoing(channel, type))
				{
					list.Add(transport);
				}
			}

			return list;
		});

		switch (transportTypes.Count)
		{
			case 0:
				if (string.IsNullOrEmpty(_options.DefaultTransporter))
				{
					throw new MessageTypeException("No transport is configured for the message type.");
				}

				transportTypes = new List<string> { _options.DefaultTransporter };
				break;

			case > 1 when !_configurator.Convention.IsMulticast(channel, type):
				throw new MessageTypeException("Multiple transports are configured for a unicast message type.");
		}

		return transportTypes;
	}
}