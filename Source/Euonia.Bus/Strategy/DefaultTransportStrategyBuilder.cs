namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于构建传输策略的构建器。
/// </summary>
public class DefaultTransportStrategyBuilder : ITransportStrategyBuilder
{
	private readonly BaseTransportStrategy _strategy = new();

	/// <summary>
	/// 正在构建的传输策略。
	/// </summary>
	public ITransportStrategy Strategy => _strategy;

	/// <summary>
	/// 定义用于决定消息如何分发的传出策略。
	/// </summary>
	/// <param name="strategy">用于评估传出通道的策略函数。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	public ITransportStrategyBuilder EvaluateOutgoing(Func<string, Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_strategy.DefineOutgoingStrategy(strategy);
		return this;
	}

	/// <summary>
	/// 定义用于决定消息如何接收的传入策略。
	/// </summary>
	/// <param name="strategy">用于评估传入通道的策略函数。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	public ITransportStrategyBuilder EvaluateIncoming(Func<string, Type, bool> strategy)
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_strategy.DefineIncomingStrategy(strategy);
		return this;
	}

	/// <summary>
	/// 添加一个传输策略实例，用于决定消息如何分发。
	/// </summary>
	/// <typeparam name="TStrategy">实现 <see cref="ITransportStrategy"/> 的策略类型。</typeparam>
	/// <param name="strategy">要添加的传输策略实例。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	public ITransportStrategyBuilder Add<TStrategy>(TStrategy strategy)
		where TStrategy : class, ITransportStrategy
	{
		ArgumentNullException.ThrowIfNull(strategy);

		_strategy.Add(strategy);
		return this;
	}

	/// <summary>
	/// 通过工厂函数添加一个传输策略实例。
	/// </summary>
	/// <typeparam name="TStrategy">实现 <see cref="ITransportStrategy"/> 的策略类型。</typeparam>
	/// <param name="strategyFactory">用于创建传输策略实例的工厂函数。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	public ITransportStrategyBuilder Add<TStrategy>(Func<TStrategy> strategyFactory) where TStrategy : class, ITransportStrategy
	{
		ArgumentNullException.ThrowIfNull(strategyFactory);

		_strategy.Add(strategyFactory());
		return this;
	}

	/// <summary>
	/// 添加一个传输策略类型，使用无参构造函数创建实例。
	/// </summary>
	/// <typeparam name="TStrategy">实现 <see cref="ITransportStrategy"/> 且具有无参构造函数的策略类型。</typeparam>
	/// <returns>返回当前的 <see cref="DefaultTransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	public ITransportStrategyBuilder Add<TStrategy>()
		where TStrategy : class, ITransportStrategy, new()
	{
		_strategy.Add(new TStrategy());
		return this;
	}
}