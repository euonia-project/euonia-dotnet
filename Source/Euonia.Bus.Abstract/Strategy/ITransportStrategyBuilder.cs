namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义传输策略构建器的接口，用于配置消息的传出和传入路由策略。
/// </summary>
public interface ITransportStrategyBuilder
{
	/// <summary>
	/// 获取构建完成的传输策略。
	/// </summary>
	ITransportStrategy Strategy { get; }

	/// <summary>
	/// 定义用于评估消息是否应从当前传输器发送的传出策略。
	/// </summary>
	/// <param name="strategy">用于判断消息是否应通过当前传输器发送的断言函数，参数为通道名称和消息类型。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	ITransportStrategyBuilder EvaluateOutgoing(Func<string, Type, bool> strategy);

	/// <summary>
	/// 定义用于评估消息是否应由当前传输器接收的传入策略。
	/// </summary>
	/// <param name="strategy">用于判断消息是否应由当前传输器接收的断言函数，参数为通道名称和消息类型。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	ITransportStrategyBuilder EvaluateIncoming(Func<string, Type, bool> strategy);

	/// <summary>
	/// 添加一个传输策略实例。
	/// </summary>
	/// <typeparam name="TStrategy">实现 <see cref="ITransportStrategy"/> 的策略类型。</typeparam>
	/// <param name="strategy">要添加的传输策略实例。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	ITransportStrategyBuilder Add<TStrategy>(TStrategy strategy)
		where TStrategy : class, ITransportStrategy;

	/// <summary>
	/// 通过工厂函数添加一个传输策略。
	/// </summary>
	/// <typeparam name="TStrategy">实现 <see cref="ITransportStrategy"/> 的策略类型。</typeparam>
	/// <param name="strategyFactory">用于创建策略实例的工厂函数。</param>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	ITransportStrategyBuilder Add<TStrategy>(Func<TStrategy> strategyFactory)
		where TStrategy : class, ITransportStrategy;

	/// <summary>
	/// 添加一个传输策略类型，使用无参构造函数创建实例。
	/// </summary>
	/// <typeparam name="TStrategy">实现 <see cref="ITransportStrategy"/> 且具有无参构造函数的策略类型。</typeparam>
	/// <returns>返回当前的 <see cref="ITransportStrategyBuilder"/> 实例，以便进行链式调用。</returns>
	ITransportStrategyBuilder Add<TStrategy>()
		where TStrategy : class, ITransportStrategy, new();
}