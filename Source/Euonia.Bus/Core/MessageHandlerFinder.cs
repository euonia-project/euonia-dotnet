using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息处理程序查找器，用于扫描类型和程序集，发现并注册消息处理程序。
/// </summary>
internal static class MessageHandlerFinder
{
	/// <summary>
	/// 消息处理程序查找回调委托。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="messageType">消息类型。</param>
	/// <param name="handler">通道处理器。</param>
	public delegate void Delegate(string channel, Type messageType, ChannelHandler handler);

	private const BindingFlags BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

	/// <summary>
	/// 从指定的类型集合中查找消息处理程序。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="types">要扫描的类型集合。</param>
	public static void Find(Delegate @delegate, IEnumerable<Type> types)
	{
		types.ForEach(type => Resolve(@delegate, type));
	}

	/// <summary>
	/// 从指定的程序集中查找消息处理程序。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	public static void Find(Delegate @delegate, params Assembly[] assemblies)
	{
		var types = assemblies.SelectMany(x => x.DefinedTypes);

		Find(@delegate, types);
	}

	/// <summary>
	/// 从指定的类型中查找消息处理程序。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="types">要扫描的类型数组。</param>
	public static void Find(Delegate @delegate, params Type[] types)
	{
		Find(@delegate, types.AsEnumerable());
	}

	/// <summary>
	/// 从指定的处理程序类型中提取消息注册信息。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="handlerType">要解析的处理程序类型。</param>
	/// <exception cref="MissingMethodException">当在类型中找不到处理方法时抛出。</exception>
	/// <exception cref="InvalidOperationException">当处理程序方法的参数签名不符合要求或订阅特性未指定通道名称时抛出。</exception>
	private static void Resolve(Delegate @delegate, Type handlerType)
	{
		if (handlerType.IsPrimitive || !handlerType.IsClass || handlerType.IsInterface || handlerType.IsAbstract)
		{
			return;
		}

		// 仅解析 IHandler<,> 接口，以获得处理程序声明的消息类型，用于确定消息路由。
		var interfaces = handlerType.GetInterfaces()
		                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandler<,>))
		                            .ToList();

		var handlerInterfaceMethods = new HashSet<MethodInfo>();
		if (interfaces.Count > 0)
		{
			foreach (var @interface in interfaces)
			{
				var messageType = @interface.GetGenericArguments()[0];
				var method = handlerType.GetMethod(nameof(IHandler<,>.HandleAsync), [messageType, typeof(IMessageContext), typeof(CancellationToken)]);
				if (method == null)
				{
					throw new MissingMethodException("The method doesn't exist.");
				}

				handlerInterfaceMethods.Add(method);

				var channel = MessageChannelResolver.Default.GetOrAddChannel(messageType);

				channel ??= messageType.FullName;
				@delegate(channel, messageType, new ChannelHandler(@interface, null));
			}
		}

		var methods = handlerType.GetMethods(BINDING_FLAGS)
		                         .Where(t => t.GetCustomAttributes<SubscribeAttribute>(false).Any() && !handlerInterfaceMethods.Contains(t))
		                         .ToList();

		foreach (var method in methods)
		{
			var parameters = method.GetParameters();

			if (parameters.Length == 0)
			{
				throw new InvalidOperationException("The handler method must contain at least one parameter");
			}

			switch (parameters.Length)
			{
				case 1 when parameters[0].ParameterType == typeof(IMessageContext) || parameters[0].ParameterType == typeof(CancellationToken):
					throw new InvalidOperationException("The first parameter of handler method must be message type");
				case 2 when parameters[1].ParameterType != typeof(IMessageContext) && parameters[1].ParameterType != typeof(CancellationToken):
					throw new InvalidOperationException("The second parameter of handler method must be MessageContext or CancellationToken if the method contains 2 parameters");
				case 3 when parameters[1].ParameterType != typeof(IMessageContext) || parameters[2].ParameterType != typeof(CancellationToken):
					throw new InvalidOperationException("The second and third parameter of handler method must be MessageContext and CancellationToken if the method contains 3 parameters");
			}

			var attributes = method.GetCustomAttributes<SubscribeAttribute>(false)
			                       .DistinctBy(t => t.Name)
			                       .ToList();
			if (attributes.Any(a => string.IsNullOrWhiteSpace(a.Name)))
			{
				throw new InvalidOperationException("The handler method must not have any SubscribeAttribute with an empty name");
			}

			foreach (var attribute in attributes)
			{
				@delegate(attribute.Name, parameters[0].ParameterType, new ChannelHandler(handlerType, method));
			}
		}
	}
}