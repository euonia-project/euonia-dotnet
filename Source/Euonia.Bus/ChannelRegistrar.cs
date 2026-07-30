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

	private ChannelRegistrar()
	{
	}

	/// <summary>
	/// 获取 <see cref="ChannelRegistrar"/> 的单例实例。
	/// </summary>
	public static ChannelRegistrar Instance => Singleton<ChannelRegistrar>.Get(() => new ChannelRegistrar());

	/// <summary>
	/// 获取已注册的通道处理器列表。
	/// </summary>
	/// <returns>返回已注册的通道处理器列表</returns>
	public static IDictionary<string, ChannelRegistration> Registrations => Instance._registrations.AsReadOnly();

	/// <summary>
	/// 获取指定通道的注册信息。
	/// </summary>
	/// <param name="channel">通道名称</param>
	/// <returns>返回指定通道的注册信息，如果通道未注册则返回空的 <see cref="Optional{T}"/></returns>
	public static Optional<ChannelRegistration> Get(string channel)
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
	public ChannelRegistrar Register(string channel, Type messageType, ChannelHandler handler)
	{
		ArgumentNullException.ThrowIfNull(channel);
		ArgumentNullException.ThrowIfNull(handler);

		var registration = _registrations.GetOrAdd(channel, _ => new ChannelRegistration(messageType));
		if (registration.MessageType == messageType)
		{
			throw new InvalidOperationException($"Channel '{channel}' is already registered");
		}

		registration.AddHandler(handler);
		return this;
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
	public ChannelRegistrar Register(string channel, Type messageType, Type handlerType, string methodName)
	{
		ArgumentNullException.ThrowIfNull(channel);
		ArgumentNullException.ThrowIfNull(handlerType);
		ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

		var registration = _registrations.GetOrAdd(channel, _ => new ChannelRegistration(messageType));
		if (registration.MessageType == messageType)
		{
			throw new InvalidOperationException($"Channel '{channel}' is already registered");
		}

		registration.AddHandler(handlerType, methodName);
		return this;
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
	public ChannelRegistrar Register(string channel, Type messageType, Type handlerType, MethodInfo method)
	{
		ArgumentNullException.ThrowIfNull(channel);
		ArgumentNullException.ThrowIfNull(handlerType);
		ArgumentNullException.ThrowIfNull(method);

		var registration = _registrations.GetOrAdd(channel, _ => new ChannelRegistration(messageType));
		if (registration.MessageType == messageType)
		{
			throw new InvalidOperationException($"Channel '{channel}' is already registered");
		}

		registration.AddHandler(new ChannelHandler(handlerType, method));
		return this;
	}

	/// <summary>
	/// 从指定的类型数组中自动扫描并注册消息处理程序。
	/// </summary>
	/// <param name="types">要扫描的类型数组。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="types"/> 为 <c>null</c> 时抛出。</exception>
	public ChannelRegistrar Registrar(params Type[] types)
	{
		ArgumentNullException.ThrowIfNull(types);

		MessageHandlerFinder.Find((c, m, h) => Register(c, m, h), types);
		return this;
	}

	/// <summary>
	/// 从指定的类型集合中自动扫描并注册消息处理程序。
	/// </summary>
	/// <param name="types">要扫描的类型集合。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="types"/> 为 <c>null</c> 时抛出。</exception>
	public ChannelRegistrar Register(IEnumerable<Type> types)
	{
		ArgumentNullException.ThrowIfNull(types);

		MessageHandlerFinder.Find((c, m, h) => Register(c, m, h), types);
		return this;
	}

	/// <summary>
	/// 从指定的程序集数组中自动扫描并注册消息处理程序。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	/// <returns>返回当前的 <see cref="ChannelRegistrar"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="assemblies"/> 为 <c>null</c> 时抛出。</exception>
	public ChannelRegistrar Register(params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(assemblies);

		MessageHandlerFinder.Find((c, m, h) => Register(c, m, h), assemblies);
		return this;
	}

	public ChannelRegistrar Register<T, R>(string channel, Func<T, IMessageContext, Task<R>> handler)
	{
		try
		{
			var method = typeof(LambdaHandler<T, R>).GetMethod(nameof(LambdaHandler<T, R>.HandleAsync), BindingFlags.Public | BindingFlags.Instance);
			return Register(channel, typeof(T), new ChannelHandler(typeof(LambdaHandler<T, R>), method, new LambdaHandler<T, R>(handler)));
		}
		catch (Exception exception)
		{
			return this;
		}
	}

	public ChannelRegistrar Register<T>(string channel, Func<T, IMessageContext, Task> handler)
	{
		try
		{
			var method = typeof(LambdaHandler<T>).GetMethod(nameof(LambdaHandler<T>.HandleAsync), BindingFlags.Public | BindingFlags.Instance);
			return Register(channel, typeof(T), new ChannelHandler(typeof(LambdaHandler<T>), method, new LambdaHandler<T>(handler)));
		}
		catch (Exception exception)
		{
			return this;
		}
	}
}