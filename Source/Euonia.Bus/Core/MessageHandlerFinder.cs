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
	/// <param name="channelResolver">用于把消息类型解析为通道名称的委托；须与分发时使用的解析器一致。</param>
	/// <param name="types">要扫描的类型集合。</param>
	public static void Find(Delegate @delegate, Func<Type, string> channelResolver, IEnumerable<Type> types)
	{
		types.ForEach(type => Resolve(@delegate, channelResolver, type));
	}

	/// <summary>
	/// 从指定的程序集中查找消息处理程序。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="channelResolver">用于把消息类型解析为通道名称的委托；须与分发时使用的解析器一致。</param>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	public static void Find(Delegate @delegate, Func<Type, string> channelResolver, params Assembly[] assemblies)
	{
		var types = assemblies.SelectMany(x => x.DefinedTypes);

		Find(@delegate, channelResolver, types);
	}

	/// <summary>
	/// 从指定的类型中查找消息处理程序。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="channelResolver">用于把消息类型解析为通道名称的委托；须与分发时使用的解析器一致。</param>
	/// <param name="types">要扫描的类型数组。</param>
	public static void Find(Delegate @delegate, Func<Type, string> channelResolver, params Type[] types)
	{
		Find(@delegate, channelResolver, types.AsEnumerable());
	}

	/// <summary>
	/// 从指定的处理程序类型中提取消息注册信息。
	/// </summary>
	/// <param name="delegate">处理程序查找回调委托。</param>
	/// <param name="channelResolver">用于把消息类型解析为通道名称的委托；须与分发时使用的解析器一致。</param>
	/// <param name="handlerType">要解析的处理程序类型。</param>
	/// <exception cref="MissingMethodException">当在类型中找不到处理方法时抛出。</exception>
	/// <exception cref="InvalidOperationException">当处理程序方法的参数签名不符合要求或订阅特性未指定通道名称时抛出。</exception>
	private static void Resolve(Delegate @delegate, Func<Type, string> channelResolver, Type handlerType)
	{
		if (handlerType.IsPrimitive || !handlerType.IsClass || handlerType.IsInterface || handlerType.IsAbstract)
		{
			return;
		}

		// 仅解析 IHandler<,> 接口，以获得处理程序声明的消息类型，用于确定消息路由。
		var interfaces = handlerType.GetInterfaces()
		                            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandler<,>))
		                            // IHandler<T> 继承 IHandler<T, Unit>：两者同时出现时只注册更具体的单参数接口，
		                            // 否则同一个处理程序会在多播通道上被执行两次。
		                            .GroupBy(x => x.GetGenericArguments()[0])
		                            .Select(group => group.OrderBy(x => x.GetGenericArguments().Length).First())
		                            .ToList();

		var handlerInterfaceMethods = new HashSet<MethodInfo>();
		if (interfaces.Count > 0)
		{
			foreach (var @interface in interfaces)
			{
				var messageType = @interface.GetGenericArguments()[0];
				var method = GetImplementationMethod(handlerType, @interface, messageType);
				if (method == null)
				{
					throw new MissingMethodException(string.Format(Resources.IDS_HANDLER_METHOD_NOT_EXIST, handlerType.FullName, nameof(IHandler<,>.HandleAsync), messageType.FullName));
				}

				handlerInterfaceMethods.Add(method);

				// 必须使用配置器提供的解析器：分发时用的是它，注册时必须用同一个，
				// 否则配置了自定义解析器后处理器会注册在类型全名上、而消息被分发到自定义通道，
				// 处理器永远匹配不到（此前这里硬编码调用进程级的 MessageChannelResolver.Default）。
				var channel = channelResolver?.Invoke(messageType);

				channel ??= messageType.FullName;
				@delegate(channel, messageType, new ChannelHandler(@interface, null, ImplementationType: handlerType));
			}
		}

		var methods = handlerType.GetMethods(BINDING_FLAGS)
		                         .Where(t => t.GetCustomAttributes<SubscribeAttribute>(false).Any() && !handlerInterfaceMethods.Contains(t))
		                         .ToList();

		foreach (var method in methods)
		{
			var parameters = method.GetParameters();
			var parameterTypes = string.Join(", ", parameters.Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name));

			if (parameters.Length == 0)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_HANDLER_METHOD_REQUIRES_PARAMETER, handlerType.FullName, method.Name));
			}

			switch (parameters.Length)
			{
				case 1 when parameters[0].ParameterType == typeof(IMessageContext) || parameters[0].ParameterType == typeof(CancellationToken):
					throw new InvalidOperationException(string.Format(Resources.IDS_HANDLER_FIRST_PARAMETER_IS_MESSAGE, handlerType.FullName, method.Name, parameterTypes));
				case 2 when parameters[1].ParameterType != typeof(IMessageContext) && parameters[1].ParameterType != typeof(CancellationToken):
					throw new InvalidOperationException(string.Format(Resources.IDS_HANDLER_SECOND_PARAMETER_IS_CONTEXT_OR_CANCEL, handlerType.FullName, method.Name, parameterTypes));
				case 3 when parameters[1].ParameterType != typeof(IMessageContext) || parameters[2].ParameterType != typeof(CancellationToken):
					throw new InvalidOperationException(string.Format(Resources.IDS_HANDLER_SECOND_THIRD_PARAMETER_ARE_CONTEXT_AND_CANCEL, handlerType.FullName, method.Name, parameterTypes));
			}

			var attributes = method.GetCustomAttributes<SubscribeAttribute>(false)
			                       .DistinctBy(t => t.Name)
			                       .ToList();
			if (attributes.Any(a => string.IsNullOrWhiteSpace(a.Name)))
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_HANDLER_SUBSCRIBE_NAME_REQUIRED, handlerType.FullName, method.Name));
			}

			foreach (var attribute in attributes)
			{
				@delegate(attribute.Name, parameters[0].ParameterType, new ChannelHandler(handlerType, method));
			}
		}
	}

	/// <summary>
	/// 获取处理程序类型对指定接口方法的实际实现。
	/// </summary>
	/// <param name="handlerType">处理程序类型。</param>
	/// <param name="interfaceType">处理程序实现的 <see cref="IHandler{TMessage,TResult}"/> 接口。</param>
	/// <param name="messageType">接口的消息类型。</param>
	/// <returns>
	/// 接口方法对应的实现方法；找不到时为 <c>null</c>。
	/// 显式接口实现的方法名带有接口前缀，不能按简单名调用 <see cref="Type.GetMethod(string, Type[])"/> 查找，
	/// 必须通过接口映射定位，否则合法的显式实现会被误判为缺少方法。
	/// </returns>
	private static MethodInfo GetImplementationMethod(Type handlerType, Type interfaceType, Type messageType)
	{
		var interfaceMethod = interfaceType.GetMethod(nameof(IHandler<,>.HandleAsync), [messageType, typeof(IMessageContext), typeof(CancellationToken)]);
		if (interfaceMethod == null)
		{
			return null;
		}

		var map = handlerType.GetInterfaceMap(interfaceType);
		var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
		return index >= 0 ? map.TargetMethods[index] : null;
	}
}