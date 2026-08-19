using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 是一个用于注册和管理通道处理器的类。
/// </summary>
/// <remarks>
///	<see cref="ChannelRegistrar"/>提供了注册通道处理器、获取已注册的通道处理器列表以及获取指定通道的注册信息的方法。
/// 该类使用单例模式，确保在整个应用程序中只有一个实例。
/// </remarks>
internal sealed class ChannelRegistrar
{
	private readonly ConcurrentDictionary<string, ChannelRegistration> _registrations = new();

	private readonly Action<string, Type, ChannelHandler> _registerAction;

	private ChannelRegistrar()
	{
	}

	public ChannelRegistrar(Action<string, Type, ChannelHandler> registerAction)
		: this()
	{
		_registerAction = registerAction;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="channel"></param>
	public ChannelRegistration this[string channel] => _registrations.GetValueOrDefault(channel);

	/// <summary>
	/// 获取已注册的通道处理器列表。
	/// </summary>
	/// <returns>返回已注册的通道处理器列表</returns>
	public IDictionary<string, ChannelRegistration> Registrations => _registrations.AsReadOnly();

	/// <summary>
	/// 获取指定通道的注册信息。
	/// </summary>
	/// <param name="channel">通道名称</param>
	/// <returns>返回指定通道的注册信息，如果通道未注册则返回空的 <see cref="Optional{T}"/></returns>
	public Optional<ChannelRegistration> Get(string channel)
	{
		var value = Registrations.TryGetValue(channel, out var registration) ? registration : null;
		return Optional<ChannelRegistration>.OfNullable(value);
	}

	/// <summary>
	/// 注册一个通道处理器。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="messageType">消息类型。</param>
	/// <param name="handler">通道处理器。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="channel"/> 或 <paramref name="handler"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当指定的通道已注册时抛出。</exception>
	public void Register(string channel, Type messageType, ChannelHandler handler)
	{
		ArgumentNullException.ThrowIfNull(channel);
		ArgumentNullException.ThrowIfNull(handler);

		var registration = _registrations.GetOrAdd(channel, _ => new ChannelRegistration(messageType));
		if (registration.MessageType != messageType)
		{
			throw new InvalidOperationException($"Channel '{channel}' is already registered with a different message type.");
		}

		registration.AddHandler(handler);
		_registerAction?.Invoke(channel, messageType, handler);
	}

	/// <summary>
	/// 注册一个通道处理器，通过处理器类型和方法名称指定处理方法。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="messageType">消息类型。</param>
	/// <param name="handlerType">处理器类型。</param>
	/// <param name="methodName">处理方法名称。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="channel"/> 或 <paramref name="handlerType"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="methodName"/> 为 <c>null</c> 或空白时抛出。</exception>
	/// <exception cref="InvalidOperationException">当指定的通道已注册时抛出。</exception>
	public void Register(string channel, Type messageType, Type handlerType, string methodName)
	{
		ArgumentNullException.ThrowIfNull(handlerType);
		var method = PriorityValueFinder.Find<MethodInfo>(queue =>
		{
			queue.Enqueue(() => handlerType.GetMethod(methodName, [messageType, typeof(IMessageContent), typeof(CancellationToken)]), 1);
			queue.Enqueue(() => handlerType.GetMethod(methodName, [messageType, typeof(IMessageContent)]), 2);
			queue.Enqueue(() => handlerType.GetMethod(methodName, [messageType, typeof(CancellationToken)]), 3);
			queue.Enqueue(() => handlerType.GetMethod(methodName, [messageType]), 4);
		}, value => value != null);

		Register(channel, messageType, handlerType, method);
	}

	/// <summary>
	/// 注册一个通道处理器，通过处理器类型和方法信息指定处理方法。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="messageType">消息类型。</param>
	/// <param name="handlerType">处理器类型。</param>
	/// <param name="method">处理方法信息。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="channel"/>、<paramref name="handlerType"/> 或 <paramref name="method"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当指定的通道已注册时抛出。</exception>
	public void Register(string channel, Type messageType, Type handlerType, MethodInfo method)
	{
		ArgumentNullException.ThrowIfNull(channel);
		ArgumentNullException.ThrowIfNull(handlerType);
		ArgumentNullException.ThrowIfNull(method);

		Register(channel, messageType, new ChannelHandler(handlerType, method));
	}

	/// <summary>
	/// 从指定的类型数组中自动扫描并注册消息处理程序。
	/// </summary>
	/// <param name="types">要扫描的类型数组。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="types"/> 为 <c>null</c> 时抛出。</exception>
	public void Register(params Type[] types)
	{
		ArgumentNullException.ThrowIfNull(types);

		MessageHandlerFinder.Find(Register, types);
	}

	/// <summary>
	/// 从指定的类型集合中自动扫描并注册消息处理程序。
	/// </summary>
	/// <param name="types">要扫描的类型集合。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="types"/> 为 <c>null</c> 时抛出。</exception>
	public void Register(IEnumerable<Type> types)
	{
		ArgumentNullException.ThrowIfNull(types);

		MessageHandlerFinder.Find(Register, types);
	}

	/// <summary>
	/// 从指定的程序集数组中自动扫描并注册消息处理程序。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="assemblies"/> 为 <c>null</c> 时抛出。</exception>
	public void Register(params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(assemblies);

		MessageHandlerFinder.Find(Register, assemblies);
	}

	/// <summary>
	/// 注册一个通道处理器，通过 Lambda 委托指定处理方法，并支持返回结果。
	/// 内部通过 <see cref="LambdaHandler{TMessage, TResult}"/> 将委托包装为处理器。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <typeparam name="TResult">处理结果的类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理消息的委托，接收消息和消息上下文，返回处理结果。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	public void Register<TMessage, TResult>(string channel, Func<TMessage, IMessageContext, Task<TResult>> handler)
	{
		var method = typeof(LambdaHandler<TMessage, TResult>).GetMethod(nameof(LambdaHandler<,>.HandleAsync), BindingFlags.Public | BindingFlags.Instance);
		Register(channel, typeof(TMessage), new ChannelHandler(typeof(LambdaHandler<TMessage, TResult>), method, new LambdaHandler<TMessage, TResult>(handler)));
	}

	/// <summary>
	/// 注册一个通道处理器，通过 Lambda 委托指定处理方法，不返回结果。
	/// 内部通过 <see cref="LambdaHandler{TMessage}"/> 将委托包装为处理器。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理消息的委托，接收消息和消息上下文。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	public void Register<TMessage>(string channel, Func<TMessage, IMessageContext, Task> handler)
	{
		var method = typeof(LambdaHandler<TMessage>).GetMethod(nameof(LambdaHandler<>.HandleAsync), BindingFlags.Public | BindingFlags.Instance);
		Register(channel, typeof(TMessage), new ChannelHandler(typeof(LambdaHandler<TMessage>), method, new LambdaHandler<TMessage>(handler)));
	}
}