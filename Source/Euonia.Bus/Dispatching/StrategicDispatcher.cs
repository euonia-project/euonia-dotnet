using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 策略化消息分发器，根据传输策略决定消息应由哪些传输器分发，并对结果进行缓存。
/// </summary>
internal class StrategicDispatcher : IDispatcher
{
	/// <summary>
	/// 「通道 + 类型 → 传输器列表」的缓存。条目携带生成时的策略版本号，
	/// 版本不一致即视为过期并重新求值。
	/// </summary>
	private readonly ConcurrentDictionary<(string Channel, Type Type), (long Version, IReadOnlyList<string> Transports)> _transportCache = new();

	private readonly IConfigurator _configurator;
	private readonly MessageBusOptions _options;

	/// <summary>
	/// 初始化 <see cref="StrategicDispatcher"/> 类的新实例。
	/// </summary>
	/// <param name="configurator">提供约定与传输策略的配置器。</param>
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
		var version = _configurator.StrategyVersion;
		var key = (channel, type);

		// 缓存需带版本校验：策略在运行期变更后（SetStrategy），此前的判定结果不再有效。
		// 之前此处是无条件永久缓存，导致后配置的传输器被静默忽略。
		if (!_transportCache.TryGetValue(key, out var cached) || cached.Version != version)
		{
			cached = (version, Resolve(channel, type, version));
			_transportCache[key] = cached;
		}

		var transports = cached.Transports;

		switch (transports.Count)
		{
			case 0:
				// Resolve 已保证无匹配且无默认传输器时抛错，因此这里不会取到空集合。
				break;

			case > 1 when !_configurator.Convention.IsMulticast(channel, type):
				throw new MessageTypeException("Multiple transports are configured for a unicast message type.");
		}

		return transports;
	}

	/// <summary>
	/// 依据当前策略求值传输器列表。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="type">消息类型。</param>
	/// <param name="version">求值时的策略版本号，用于回填缓存。</param>
	/// <returns>负责分发该通道消息的传输器名称集合。</returns>
	/// <exception cref="MessageTypeException">当无任何传输器匹配且未配置默认传输器时抛出。</exception>
	private IReadOnlyList<string> Resolve(string channel, Type type, long version)
	{
		var list = new List<string>();
		foreach (var transport in _configurator.StrategyAssignedTypes)
		{
			var strategy = _configurator.GetStrategy(transport);
			if (strategy != null && strategy.Outgoing(channel, type))
			{
				list.Add(transport);
			}
		}

		if (list.Count == 0)
		{
			if (string.IsNullOrEmpty(_options.DefaultTransporter))
			{
				throw new MessageTypeException($"No transport is configured for the message type '{type.FullName}' on channel '{channel}', and no default transporter is configured.");
			}

			// 默认传输器的回退结果同样缓存：此前每次调用都要重新分配一个列表。
			list.Add(_options.DefaultTransporter);
		}

		return list;
	}
}
