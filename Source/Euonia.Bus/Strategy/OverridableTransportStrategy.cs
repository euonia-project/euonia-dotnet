namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents a transport strategy that allows overriding the behavior of an inner transport strategy
/// for outgoing and incoming message evaluations.
/// </summary>
internal class OverridableTransportStrategy : ITransportStrategy
{
    private readonly ITransportStrategy _innerStrategy;
    private Func<Type, bool> _outgoingEvaluator, _incomingEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="OverridableTransportStrategy"/> class.
    /// </summary>
    /// <param name="innerStrategy">The inner transport strategy to be overridden.</param>
    public OverridableTransportStrategy(ITransportStrategy innerStrategy)
    {
        _innerStrategy = innerStrategy;
    }

    /// <summary>
    /// Gets the name of the transport strategy, including the name of the inner strategy.
    /// </summary>
    public string Name => $"Override with {_innerStrategy.Name}";

    /// <summary>
    /// Determines whether the specified message type can be dispatched by the transport.
    /// Delegates to the outgoing evaluator or the inner strategy if no evaluator is defined.
    /// </summary>
    /// <param name="messageType">The type of the message to evaluate.</param>
    /// <returns><c>true</c> if the message type is allowed for outgoing; otherwise, <c>false</c>.</returns>
    bool ITransportStrategy.Outgoing(Type messageType)
    {
        return Outgoing(messageType);
    }

    /// <summary>
    /// Determines whether the specified message type can be received by the transport.
    /// Delegates to the incoming evaluator or the inner strategy if no evaluator is defined.
    /// </summary>
    /// <param name="messageType">The type of the message to evaluate.</param>
    /// <returns><c>true</c> if the message type is allowed for incoming; otherwise, <c>false</c>.</returns>
    bool ITransportStrategy.Incoming(Type messageType)
    {
        return Incoming(messageType);
    }

    /// <summary>
    /// Gets or sets the outgoing evaluator function, which determines if a message type is allowed for outgoing.
    /// If not set, the inner strategy's outgoing evaluation is used.
    /// </summary>
    public Func<Type, bool> Outgoing
    {
        get => _outgoingEvaluator ?? _innerStrategy.Outgoing;
        set => _outgoingEvaluator = value;
    }

    /// <summary>
    /// Gets or sets the incoming evaluator function, which determines if a message type is allowed for incoming.
    /// If not set, the inner strategy's incoming evaluation is used.
    /// </summary>
    public Func<Type, bool> Incoming
    {
        get => _incomingEvaluator ?? _innerStrategy.Incoming;
        set => _incomingEvaluator = value;
    }

    /// <summary>
    /// Defines a custom strategy for evaluating outgoing message types.
    /// </summary>
    /// <param name="strategy">The function to evaluate outgoing message types.</param>
    public void DefineOutgoingStrategy(Func<Type, bool> strategy)
    {
        _outgoingEvaluator = strategy;
    }

    /// <summary>
    /// Defines a custom strategy for evaluating incoming message types.
    /// </summary>
    /// <param name="strategy">The function to evaluate incoming message types.</param>
    public void DefineIncomingStrategy(Func<Type, bool> strategy)
    {
        _incomingEvaluator = strategy;
    }
}