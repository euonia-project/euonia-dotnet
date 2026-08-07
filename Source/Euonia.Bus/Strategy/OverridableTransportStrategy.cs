namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示一个可覆盖的传输策略，允许覆盖内部传输策略的传出和传入消息评估行为。
/// </summary>
internal class OverridableTransportStrategy : ITransportStrategy
{
	private readonly ITransportStrategy _innerStrategy;
	private Func<string, Type, bool> _outgoingEvaluator, _incomingEvaluator;

	/// <summary>
	/// 初始化 <see cref="OverridableTransportStrategy"/> 类的新实例。
	/// </summary>
	/// <param name="innerStrategy">要被覆盖的内部传输策略。</param>
	public OverridableTransportStrategy(ITransportStrategy innerStrategy)
	{
		_innerStrategy = innerStrategy;
	}

	/// <summary>
	/// 获取传输策略的名称，包含内部策略的名称。
	/// </summary>
	public string Name => $"Override with {_innerStrategy.Name}";

	/// <summary>
	/// 判断指定的消息通道是否可以由此传输策略进行传出操作。
	/// 委托给传出评估函数执行，如果未定义则使用内部策略的评估。
	/// </summary>
	/// <param name="channel">要评估的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果消息通道允许传出，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool ITransportStrategy.Outgoing(string channel, Type type)
	{
		return Outgoing(channel, type);
	}

	/// <summary>
	/// 判断指定的消息通道是否可以由此传输策略进行传入操作。
	/// 委托给传入评估函数执行，如果未定义则使用内部策略的评估。
	/// </summary>
	/// <param name="channel">要评估的通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果消息通道允许传入，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool ITransportStrategy.Incoming(string channel, Type type)
	{
		return Incoming(channel, type);
	}

	/// <summary>
	/// 获取或设置传出评估函数，用于判断消息通道是否允许传出。
	/// 若未设置，则回退到内部策略的传出评估。
	/// </summary>
	public Func<string, Type, bool> Outgoing
	{
		get => _outgoingEvaluator ?? _innerStrategy.Outgoing;
		set => _outgoingEvaluator = value;
	}

	/// <summary>
	/// 获取或设置传入评估函数，用于判断消息通道是否允许传入。
	/// 若未设置，则回退到内部策略的传入评估。
	/// </summary>
	public Func<string, Type, bool> Incoming
	{
		get => _incomingEvaluator ?? _innerStrategy.Incoming;
		set => _incomingEvaluator = value;
	}

	/// <summary>
	/// 定义用于评估传出通道的自定义策略。
	/// </summary>
	/// <param name="strategy">用于评估传出通道的函数。</param>
	public void DefineOutgoingStrategy(Func<string, Type, bool> strategy)
	{
		_outgoingEvaluator = strategy;
	}

	/// <summary>
	/// 定义用于评估传入通道的自定义策略。
	/// </summary>
	/// <param name="strategy">用于评估传入通道的函数。</param>
	public void DefineIncomingStrategy(Func<string, Type, bool> strategy)
	{
		_incomingEvaluator = strategy;
	}
}