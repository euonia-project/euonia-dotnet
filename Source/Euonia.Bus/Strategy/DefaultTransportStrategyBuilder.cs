namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Builds a transport strategy.
/// </summary>
public class TransportStrategyBuilder
{
	/// <summary>
	/// The transport strategy being built.
	/// </summary>
	public BaseTransportStrategy Strategy { get; } = new();

	/// <summary>
	/// Adds a transport strategy that will be used to determine how messages are dispatched.
	/// </summary>
	/// <param name="strategy"></param>
	/// <returns></returns>
	public TransportStrategyBuilder EvaluateOutgoing(Func<Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		Strategy.DefineOutgoingStrategy(strategy);
		return this;
	}

	/// <summary>
	/// Adds a transport strategy that will be used to determine how messages are received.
	/// </summary>
	/// <param name="strategy"></param>
	/// <returns></returns>
	public TransportStrategyBuilder EvaluateIncoming(Func<Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		Strategy.DefineIncomingStrategy(strategy);
		return this;
	}

	/// <summary>
	/// Adds a transport strategy that will be used to determine how messages are dispatched.
	/// </summary>
	/// <param name="strategy"></param>
	/// <typeparam name="TStrategy"></typeparam>
	/// <returns></returns>
	public TransportStrategyBuilder Add<TStrategy>(TStrategy strategy)
		where TStrategy : class, ITransportStrategy
	{
		ArgumentNullException.ThrowIfNull(strategy);

		Strategy.Add(strategy);
		return this;
	}

	/// <summary>
	/// Adds a transport strategy that will be used to determine how messages are dispatched.
	/// </summary>
	/// <typeparam name="TStrategy"></typeparam>
	/// <returns></returns>
	public TransportStrategyBuilder Add<TStrategy>()
		where TStrategy : class, ITransportStrategy, new()
	{
		Strategy.Add(new TStrategy());
		return this;
	}
}