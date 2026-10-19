using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 使用 Microsoft 依赖注入的默认消息处理程序上下文。
/// </summary>
internal sealed class DefaultHandlerContext : IHandlerContext, IDisposable
{
	/// <summary>
	/// 当消息处理程序被订阅时触发。
	/// </summary>
	public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed;

	private readonly ConcurrentDictionary<string, List<HandlerRegistration>> _handlerContainer = new();

	/// <summary>
	/// 保护处理器列表的注册与快照，避免注册与消息处理并发时枚举到正在修改的列表。
	/// </summary>
	private readonly object _registrationLock = new();
	private readonly ILogger<DefaultHandlerContext> _logger;
	private readonly MessageExecutionEnvironment _execution;
	private readonly IConfigurator _configurator;

	private readonly InboxOptions _inboxOptions;
	private readonly InboxDispatcher _inboxDispatcher;

	private bool _disposed;

	private IMessageConvention Convention => field ??= _configurator?.Convention ?? new BaseMessageConvention();

	/// <summary>
	/// 初始化 <see cref="DefaultHandlerContext"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析处理程序、日志记录器和其他服务的服务提供程序。</param>
	/// <param name="configurator">用于配置消息总线的配置器。</param>
	public DefaultHandlerContext(IServiceProvider provider, IConfigurator configurator)
	{
		_configurator = configurator;
		_logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultHandlerContext>();
		_execution = new MessageExecutionEnvironment(provider, _logger);
		_configurator.ChannelRegistered += OnChannelRegistered;

		_inboxOptions = provider.GetService<IOptions<MessageBusOptions>>()?.Value?.Inbox ?? new InboxOptions();
		// 收件箱存储按轮询/处理作用域解析；单例构造期不捕获可能为 scoped 的存储。
		_inboxDispatcher = new InboxDispatcher(provider, null, _inboxOptions, _handlerContainer);
		_inboxDispatcher.Start();
	}

	#region Handling register

	private void OnChannelRegistered(object sender, ChannelRegisteredEventArgs args)
	{
		if (args.Handler.HandlerType.IsInterface && args.Handler.HandlerType.IsGenericType && args.Handler.HandlerType.GetGenericTypeDefinition() == typeof(IHandler<,>))
		{
			// 以具体实现类型注册：同一个接口的多个实现必须各自成为独立的处理器，
			// 否则去重逻辑会把它们合并，且 DI 的 GetRequiredService(interface) 只能解析到最后一个实现。
			var implementationType = args.Handler.ImplementationType ?? args.Handler.HandlerType;

			typeof(DefaultHandlerContext).GetMethod(nameof(Register), 3, BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string)])
			                             ?.MakeGenericMethod(args.Type, args.Handler.HandlerType.GenericTypeArguments[1], implementationType)
			                             .Invoke(this, [args.Channel]);
		}
		else
		{
			Register(args.Channel, args.Handler.HandlerType, args.Handler.Instance, args.Handler.Method);
		}
	}

	/// <summary>
	/// 为消息类型 <typeparamref name="TMessage"/> 注册一个消息处理程序类型。
	/// </summary>
	/// <typeparam name="TMessage">要处理的消息类型，必须是引用类型。</typeparam>
	/// <typeparam name="TResponse">处理程序返回的响应类型。</typeparam>
	/// <typeparam name="THandler">实现了 <see cref="IHandler{TMessage}"/> 的处理程序类型。</typeparam>
	internal void Register<TMessage, TResponse, THandler>(string channel)
		where TMessage : class
		where THandler : IHandler<TMessage, TResponse>
	{
		HandlerDelegate Handling(IServiceProvider provider)
		{
			// 容器里可能未注册具体实现类型（仅通过 configurator 扫描时），回退到运行时创建。
			var handler = ActivatorUtilities.GetServiceOrCreateInstance<THandler>(provider);
			return async (message, context, token) => await handler.HandleAsync((TMessage)message, context, token);
		}

		AddRegistration(channel, typeof(THandler).FullName, Handling);
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, typeof(TMessage), typeof(THandler)));
	}

	private void Register(string channel, Type type, object instance, MethodInfo method)
	{
		var invoker = BuildHandlerInvoker(method);
		if (invoker == null)
		{
			_logger.LogWarning("Handler method {HandlerType}.{Method} on channel {Channel} has more than three parameters and cannot be registered", type.FullName, method.Name, channel);
			return;
		}

		HandlerDelegate Handling(IServiceProvider provider)
		{
			var handler = instance ?? ActivatorUtilities.GetServiceOrCreateInstance(provider, type);

			return (message, context, token) => invoker(handler, message, context, token);
		}

		AddRegistration(channel, type.FullName, Handling);
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, null, type));
	}

	/// <summary>
	/// 注册由 <see cref="ChannelHandler"/> 描述的处理程序。
	/// 注册信息包含处理程序类型、要调用的方法和通道名称。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="channelHandler">描述要注册的处理程序的 <see cref="ChannelHandler"/> 实例。</param>
	private void Register(string channel, ChannelHandler channelHandler)
	{
		HandlerFactory handling;

		if (channelHandler.HandlerType.IsInterface && channelHandler.HandlerType.IsGenericType && channelHandler.HandlerType.GetGenericTypeDefinition() == typeof(IHandler<,>))
		{
			var messageType = channelHandler.HandlerType.GenericTypeArguments[0];
			var handleAsyncMethod = channelHandler.HandlerType.GetMethod(nameof(IHandler<,>.HandleAsync), [messageType, typeof(IMessageContext), typeof(CancellationToken)])!;

			var handlerParam = Expression.Parameter(typeof(object), "handler");
			var messageParam = Expression.Parameter(typeof(object), "message");
			var contextParam = Expression.Parameter(typeof(IMessageContext), "context");
			var tokenParam = Expression.Parameter(typeof(CancellationToken), "token");

			var call = Expression.Call(
				Expression.Convert(handlerParam, channelHandler.HandlerType),
				handleAsyncMethod,
				Expression.Convert(messageParam, messageType),
				contextParam,
				tokenParam);

			var invoker = Expression.Lambda<Func<object, object, IMessageContext, CancellationToken, Task<object>>>(
				MethodInvokerBuilder.WrapToTaskObject(call, handleAsyncMethod.ReturnType),
				handlerParam, messageParam, contextParam, tokenParam).Compile();

			handling = provider =>
			{
				var handler = provider.GetRequiredService(channelHandler.HandlerType);
				return (message, context, token) => invoker(handler, message, context, token);
			};
		}
		else
		{
			var invoker = BuildHandlerInvoker(channelHandler.Method);
			if (invoker == null)
			{
				_logger.LogWarning("Handler method {HandlerType}.{Method} on channel {Channel} has more than three parameters and cannot be registered", channelHandler.HandlerType.FullName, channelHandler.Method.Name, channel);
				return;
			}

			handling = provider =>
			{
				var instance = channelHandler.Instance ?? ActivatorUtilities.GetServiceOrCreateInstance(provider, channelHandler.HandlerType);

				return (message, context, token) => invoker(instance, message, context, token);
			};
		}

		AddRegistration(channel, (channelHandler.ImplementationType ?? channelHandler.HandlerType).FullName, handling);
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, null, channelHandler.HandlerType));
	}

	/// <summary>
	/// 向通道追加一个处理器注册记录；同名记录自动追加序号，避免同类型 Lambda 处理器互相覆盖。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="name">处理器名称。</param>
	/// <param name="factory">处理器工厂。</param>
	private void AddRegistration(string channel, string name, HandlerFactory factory)
	{
		var registrations = _handlerContainer.GetOrAdd(channel, _ => []);

		lock (_registrationLock)
		{
			var uniqueName = name;
			var suffix = 2;

			while (registrations.Any(item => string.Equals(item.Name, uniqueName, StringComparison.Ordinal)))
			{
				uniqueName = $"{name}#{suffix}";
				suffix++;
			}

			registrations.Add(new HandlerRegistration(uniqueName, factory));
		}
	}

	/// <summary>
	/// 重放一条收件箱死信：只执行指定处理器，跳过收件箱去重，并在成功后标记该处理器为成功。
	/// </summary>
	/// <param name="channel">消息通道。</param>
	/// <param name="message">消息负载。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="handler">处理器名称（死信记录的 <c>Target</c>）。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示重放操作的任务。</returns>
	internal async Task ReplayDeadLetterAsync(string channel, object message, MessageContext context, string handler, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(message);
		ArgumentException.ThrowIfNullOrWhiteSpace(handler);

		HandlerRegistration registration;
		lock (_registrationLock)
		{
			if (!_handlerContainer.TryGetValue(channel, out var registrations) || registrations == null)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_NO_HANDLER_REGISTERED, context.MessageId, channel));
			}

			registration = registrations.FirstOrDefault(item => string.Equals(item.Name, handler, StringComparison.Ordinal));
		}

		if (registration == null)
		{
			throw new InvalidOperationException(string.Format(Resources.IDS_NO_HANDLER_REGISTERED, context.MessageId, channel));
		}

		// 重放按原消息的身份执行，而不是触发重放的管理请求身份。
		await _execution.RunAsync(
			MessageExecutionDescriptor.Replay,
			context,
			async provider =>
			{
				var invoker = registration.Factory(provider);
				var result = await invoker(message, context, cancellationToken).ConfigureAwait(false);
				context.Response(result);
				context.Complete(null);
				provider.GetService<IInboxStore>()?.MarkAsSuccess(context.MessageId, registration.Name);
			}).ConfigureAwait(false);
	}

	#endregion

	#region Handle message

	/// <summary>
	/// 异步处理指定通道上的消息。根据消息约定（单播/多播）选择单个处理程序或并行执行所有处理程序。
	/// </summary>
	/// <param name="channel">消息通道。</param>
	/// <param name="message">要处理的消息。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示消息处理异步操作的任务。</returns>
	public Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(message);

		return _execution.RunAsync(
			context.ExecutionDescriptor,
			context,
			provider => HandleCoreAsync(channel, message, context, provider, cancellationToken));
	}

	/// <summary>
	/// 在已建立的执行作用域中查找并调用处理程序。
	/// </summary>
	/// <param name="channel">消息通道。</param>
	/// <param name="message">要处理的消息。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="provider">本次处理实际使用的服务提供程序。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示消息处理异步操作的任务。</returns>
	private async Task<object> HandleCoreAsync(string channel, object message, MessageContext context, IServiceProvider provider, CancellationToken cancellationToken)
	{
		List<HandlerRegistration> registrations;
		lock (_registrationLock)
		{
			if (!_handlerContainer.TryGetValue(channel, out var registered) || registered == null || registered.Count == 0)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_NO_HANDLER_REGISTERED, context.MessageId, channel));
			}

			registrations = [.. registered];
		}

		// 从服务提供程序获取处理程序实例
		_logger.LogInformation("Message {Id} is being handled", context.MessageId);

		// 收件箱全局开关启用时要求已注册收件箱存储；否则视为配置缺失，快速失败以避免静默退化。
		var useInbox = _inboxOptions.Enabled;
		var inboxStore = useInbox ? provider.GetService<IInboxStore>() : null;
		if (useInbox && inboxStore == null)
		{
			throw new MessagePersistentException(Resources.IDS_INBOX_STORE_NOT_REGISTERED);
		}

		object result;

		var handlers = registrations.Select(registration => (Name: registration.Name, Handler: registration.Factory(provider))).ToList();

		if (!Convention.IsMulticast(channel, message.GetType()))
		{
			// 单播语义只允许一个处理程序，但注册侧并不阻止在同一个通道上注册多个。
			// 静默丢弃多余的处理器会让"注册了却没生效"极难排查，因此显式告警。
			if (handlers.Count > 1)
			{
				_logger.LogWarning("Channel {Channel} is a unicast channel but has {Count} handlers registered; only the first one ({Handler}) will be invoked. Ignored handlers: {Ignored}", channel, handlers.Count, handlers[0].Name, string.Join(", ", handlers.Skip(1).Select(item => item.Name)));
			}

			// 单播：执行第一个处理程序；启用收件箱时标记执行结果（不做去重跳过 —— 与 Java 版本保持一致）。
			var (name, handler) = handlers[0];
			if (useInbox)
			{
				try
				{
					result = await handler(message, context, cancellationToken);
					inboxStore.MarkAsSuccess(context.MessageId, name);
				}
				catch (Exception exception)
				{
					inboxStore.MarkAsFailed(context.MessageId, name, exception.Message);
					throw;
				}
			}
			else
			{
				result = await handler(message, context, cancellationToken);
			}
		}
		else
		{
			// 多播：启用收件箱时先写入去重记录；若记录已存在（消息被重复投递）则直接跳过；
			// 否则并行执行所有处理程序，并逐处理程序标记执行结果。
			var names = handlers.Select(handler => handler.Name).ToArray();

			if (useInbox && !inboxStore.Insert(channel, new InboundEnvelope(context, channel, message), names))
			{
				_logger.LogInformation("Message {Id} was already handled and will be skipped", context.MessageId);
				result = Unit.Value;
			}
			else
			{
				result = await Parallel.ForEachAsync(handlers, cancellationToken, async (handler, token) =>
				{
					try
					{
						await handler.Handler(message, context, token);
						if (useInbox)
						{
							inboxStore.MarkAsSuccess(context.MessageId, handler.Name);
						}
					}
					catch (Exception exception)
					{
						if (useInbox)
						{
							inboxStore.MarkAsFailed(context.MessageId, handler.Name, exception.Message);
						}

						// 多播不因单个处理器失败而中断，但必须留下日志，否则失败会被完全吞掉。
						_logger.LogError(exception, "Handler {Handler} failed while handling message {Id} on channel {Channel}", handler.Name, context.MessageId, channel);
					}
				}).ContinueWith(_ => Unit.Value, cancellationToken);
			}
		}

		_logger.LogInformation("Message {Id} was completed handled", context.MessageId);

		return result;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_inboxDispatcher?.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Supports

	/// <summary>
	/// 为指定的处理程序方法构建一次性编译的调用器。
	/// </summary>
	/// <param name="method">要调用的处理程序方法。</param>
	/// <returns>
	/// 返回可复用的 <c>Func&lt;object, object, IMessageContext, CancellationToken, Task&lt;object&gt;&gt;</c> 委托；
	/// 当方法参数超过三个（不支持）时返回 <c>null</c>。
	/// </returns>
	private static Func<object, object, IMessageContext, CancellationToken, Task<object>> BuildHandlerInvoker(MethodInfo method)
	{
		var instanceParam = Expression.Parameter(typeof(object), "instance");
		var messageParam = Expression.Parameter(typeof(object), "message");
		var contextParam = Expression.Parameter(typeof(IMessageContext), "context");
		var tokenParam = Expression.Parameter(typeof(CancellationToken), "token");

		var arguments = GetArguments(method, messageParam, contextParam, tokenParam);
		if (arguments == null)
		{
			return null;
		}

		var call = MethodInvokerBuilder.BuildCallExpression(instanceParam, method, arguments);
		return Expression.Lambda<Func<object, object, IMessageContext, CancellationToken, Task<object>>>(
			call, instanceParam, messageParam, contextParam, tokenParam).Compile();
	}

	/// <summary>
	/// 构建用于调用处理程序方法的 <see cref="Expression"/> 参数数组。
	/// 该方法最多支持三个参数，参数位置根据类型解析：
	/// - 匹配 <see cref="CancellationToken"/> 类型的参数将接收传入的 <paramref name="token"/> 表达式。
	/// - 匹配 <see cref="IMessageContext"/>（或其具体类型）的参数将接收传入的 <paramref name="context"/> 表达式。
	/// - 其余任何参数将接收 <paramref name="message"/> 表达式。
	/// </summary>
	/// <param name="method">表示要调用的处理程序方法的 <see cref="MethodInfo"/>。</param>
	/// <param name="message">表示要传递给处理程序的消息对象的表达式。</param>
	/// <param name="context">表示要传递给处理程序的 <see cref="IMessageContext"/> 的表达式。</param>
	/// <param name="token">表示要传递给处理程序的 <see cref="CancellationToken"/> 的表达式。</param>
	/// <returns>
	/// 与方法参数对应的 <see cref="Expression"/> 数组；当方法参数超过三个（不支持）时返回 <c>null</c>。
	/// </returns>
	private static Expression[] GetArguments(MethodInfo method, Expression message, Expression context, Expression token)
	{
		var parameterInfos = method.GetParameters();
		if (parameterInfos.Length > 3)
		{
			return null;
		}

		var arguments = new Expression[parameterInfos.Length];
		for (var index = 0; index < parameterInfos.Length; index++)
		{
			var parameterType = parameterInfos[index].ParameterType;

			Expression argument;
			if (parameterType == typeof(CancellationToken))
			{
				argument = token;
			}
			else if (parameterType == typeof(IMessageContext) || context.Type.IsAssignableFrom(parameterType))
			{
				argument = context;
			}
			else
			{
				argument = message;
			}

			arguments[index] = argument.Type != parameterType ? Expression.Convert(argument, parameterType) : argument;
		}

		return arguments;
	}

	#endregion
}