using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents a composite transport strategy that combines multiple transport strategies
/// and provides caching for outgoing and incoming message evaluations.
/// </summary>
public class BaseTransportStrategy : ITransportStrategy
{
	private readonly OverridableTransportStrategy _defaultStrategy = new(new DefaultTransportStrategy());
	private readonly List<ITransportStrategy> _strategies = [];
	private readonly StrategyCache _outgoingCache = new();
	private readonly StrategyCache _incomingCache = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseTransportStrategy"/> class.
	/// Adds the default transport strategy to the list of strategies.
	/// </summary>
	public BaseTransportStrategy()
	{
		_strategies.Add(_defaultStrategy);
	}

	/// <summary>
	/// Gets the name of the transport strategy.
	/// </summary>
	public string Name => "Composite transport strategy";

	/// <summary>
	/// Determines whether the specified message type can be dispatched by any of the transport strategies.
	/// Uses a cache to optimize repeated evaluations.
	/// </summary>
	/// <param name="messageType">The type of the message to evaluate.</param>
	/// <returns><c>true</c> if any strategy allows the message type for outgoing; otherwise, <c>false</c>.</returns>
	public bool Outgoing(Type messageType)
	{
		ArgumentNullException.ThrowIfNull(messageType);
		return _outgoingCache.Apply(messageType, handle =>
		{
			var type = Type.GetTypeFromHandle(handle);
			return _strategies.Any(strategy => strategy.Outgoing(type));
		});
	}

	/// <summary>
	/// Determines whether the specified message type can be received by any of the transport strategies.
	/// Uses a cache to optimize repeated evaluations.
	/// </summary>
	/// <param name="messageType">The type of the message to evaluate.</param>
	/// <returns><c>true</c> if any strategy allows the message type for incoming; otherwise, <c>false</c>.</returns>
	public bool Incoming(Type messageType)
	{
		ArgumentNullException.ThrowIfNull(messageType);
		return _incomingCache.Apply(messageType, handle =>
		{
			var type = Type.GetTypeFromHandle(handle);
			return _strategies.Any(strategy => strategy.Incoming(type));
		});
	}

	/// <summary>
	/// Adds one or more transport strategies to the composite strategy.
	/// </summary>
	/// <param name="strategies">The transport strategies to add.</param>
	/// <exception cref="ArgumentException">Thrown if no strategies are provided.</exception>
	internal void Add(params ITransportStrategy[] strategies)
	{
		if (strategies == null || strategies.Length == 0)
		{
			throw new ArgumentException(@"At least one strategy is required.", nameof(strategies));
		}

		_strategies.AddRange(strategies);
	}

	/// <summary>
	/// Defines a custom strategy for evaluating incoming message types.
	/// </summary>
	/// <param name="strategy">The function to evaluate incoming message types.</param>
	/// <exception cref="ArgumentNullException">Thrown if the strategy is null.</exception>
	internal void DefineIncomingStrategy(Func<Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_defaultStrategy.DefineIncomingStrategy(strategy);
	}

	/// <summary>
	/// Defines a custom strategy for evaluating outgoing message types.
	/// </summary>
	/// <param name="strategy">The function to evaluate outgoing message types.</param>
	/// <exception cref="ArgumentNullException">Thrown if the strategy is null.</exception>
	internal void DefineOutgoingStrategy(Func<Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_defaultStrategy.DefineOutgoingStrategy(strategy);
	}

	/// <summary>
	/// Resets the caches for outgoing and incoming message evaluations.
	/// </summary>
	internal void ResetCache()
	{
		_outgoingCache.Reset();
		_incomingCache.Reset();
	}

	/// <summary>
	/// Represents a cache for storing the results of message type evaluations.
	/// </summary>
	private class StrategyCache
	{
		private readonly ConcurrentDictionary<RuntimeTypeHandle, bool> _cache = new();

		/// <summary>
		/// Applies the specified strategy to the given message type and caches the result.
		/// </summary>
		/// <param name="messageType">The type of the message to evaluate.</param>
		/// <param name="strategy">The strategy to apply.</param>
		/// <returns>The cached or newly computed result of the strategy.</returns>
		public bool Apply(Type messageType, Func<RuntimeTypeHandle, bool> strategy)
		{
			return _cache.GetOrAdd(messageType.TypeHandle, strategy);
		}

		/// <summary>
		/// Clears the cache.
		/// </summary>
		public void Reset()
		{
			_cache.Clear();
		}
	}
}