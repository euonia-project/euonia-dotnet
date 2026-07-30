namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public delegate Task MessageHandler<in TMessage>(TMessage message, IMessageContext context, CancellationToken cancellationToken = default)
	where TMessage : class;

/// <summary>
/// 
/// </summary>
public delegate Task<object> MessageHandler(object message, IMessageContext context, CancellationToken cancellationToken = default);