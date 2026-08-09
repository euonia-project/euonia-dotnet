using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示一个组合传输策略，将多个传输策略组合在一起，并为传出和传入消息的评估提供缓存支持。
/// </summary>
public class BaseTransportStrategy : ITransportStrategy
{
	private readonly OverridableTransportStrategy _defaultStrategy = new(new DefaultTransportStrategy());
	private readonly List<ITransportStrategy> _strategies = [];
	private readonly StrategyCache _outgoingCache = new();
	private readonly StrategyCache _incomingCache = new();

	/// <summary>
	/// 初始化 <see cref="BaseTransportStrategy"/> 类的新实例，并将默认传输策略添加到策略列表中。
	/// </summary>
	public BaseTransportStrategy()
	{
		_strategies.Add(_defaultStrategy);
	}

	/// <summary>
	/// 获取传输策略的名称。
	/// </summary>
	public string Name => "Composite transport strategy";

	/// <summary>
	/// 判断指定的通道是否可以通过任意传输策略进行传出操作。
	/// 使用缓存来优化重复的评估。
	/// </summary>
	/// <param name="channel">要评估的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果有任意策略允许该通道传出，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Outgoing(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);
		return _outgoingCache.Apply(channel, type, (key, t) =>
		{
			return _strategies.Any(strategy => strategy.Outgoing(key, t));
		});
	}

	/// <summary>
	/// 判断指定的通道是否可以通过任意传输策略进行传入操作。
	/// 使用缓存来优化重复的评估。
	/// </summary>
	/// <param name="channel">要评估的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果有任意策略允许该通道传入，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Incoming(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);
		return _incomingCache.Apply(channel, type, (key, t) =>
		{
			return _strategies.Any(strategy => strategy.Incoming(key, t));
		});
	}

	/// <summary>
	/// 向组合策略中添加一个或多个传输策略。
	/// </summary>
	/// <param name="strategies">要添加的传输策略数组。</param>
	/// <exception cref="ArgumentException">当未提供任何策略时抛出。</exception>
	internal void Add(params ITransportStrategy[] strategies)
	{
		if (strategies == null || strategies.Length == 0)
		{
			throw new ArgumentException(@"At least one strategy is required.", nameof(strategies));
		}

		_strategies.AddRange(strategies);
	}

	/// <summary>
	/// 定义用于评估传入通道的自定义策略。
	/// </summary>
	/// <param name="strategy">用于评估传入通道的函数。</param>
	/// <exception cref="ArgumentNullException">当策略函数为 <c>null</c> 时抛出。</exception>
	internal void DefineIncomingStrategy(Func<string, Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_defaultStrategy.DefineIncomingStrategy(strategy);
	}

	/// <summary>
	/// 定义用于评估传出通道的自定义策略。
	/// </summary>
	/// <param name="strategy">用于评估传出通道的函数。</param>
	/// <exception cref="ArgumentNullException">当策略函数为 <c>null</c> 时抛出。</exception>
	internal void DefineOutgoingStrategy(Func<string, Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_defaultStrategy.DefineOutgoingStrategy(strategy);
	}

	/// <summary>
	/// 重置传出和传入消息评估的缓存。
	/// </summary>
	internal void ResetCache()
	{
		_outgoingCache.Reset();
		_incomingCache.Reset();
	}

	/// <summary>
	/// 表示用于存储消息通道评估结果的缓存。
	/// </summary>
	private class StrategyCache
	{
		private readonly ConcurrentDictionary<string, bool> _cache = new();

		/// <summary>
		/// 将指定的策略应用到给定的通道上，并缓存结果。
		/// </summary>
		/// <param name="channel">要评估的通道名称。</param>
		/// <param name="type">要检查的消息类型。</param>
		/// <param name="strategy">要应用的策略函数。</param>
		/// <returns>缓存或新计算出的策略结果。</returns>
		public bool Apply(string channel, Type type, Func<string, Type, bool> strategy)
		{
			return _cache.GetOrAdd(channel, key => strategy(key, type));
		}

		/// <summary>
		/// 清空缓存。
		/// </summary>
		public void Reset()
		{
			_cache.Clear();
		}
	}
}