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
	private readonly IServiceProvider _provider;
	private readonly ILogger<DefaultHandlerContext> _logger;
	private readonly IConfigurator _configurator;

	private readonly IInboxStore _inboxStore;
	private readonly InboxOptions _inboxOptions;
	private readonly InboxDispatcher _inboxDispatcher;

	private bool _disposed;

	private IMessageConvention Convention => field ??= new Lazy<IMessageConvention>(() => _configurator?.Convention ?? new BaseMessageConvention()).Value;

	/// <summary>
	/// 初始化 <see cref="DefaultHandlerContext"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析处理程序、日志记录器和其他服务的服务提供程序。</param>
	/// <param name="configurator">用于配置消息总线的配置器。</param>
	public DefaultHandlerContext(IServiceProvider provider, IConfigurator configurator)
	{
		_provider = provider;
		_configurator = configurator;
		_logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultHandlerContext>();
		_configurator.ChannelRegistered += OnChannelRegistered;

		_inboxStore = provider.GetService<IInboxStore>();
		_inboxOptions = provider.GetService<IOptions<MessageBusOptions>>()?.Value?.Inbox ?? new InboxOptions();
		_inboxDispatcher = new InboxDispatcher(provider, _inboxStore, _inboxOptions, _handlerContainer);
		_inboxDispatcher.Start();
	}

	#region Handling register

	private void OnChannelRegistered(object sender, ChannelRegisteredEventArgs args)
	{
		if (args.Handler.HandlerType.IsInterface && args.Handler.HandlerType.IsGenericType && args.Handler.HandlerType.GetGenericTypeDefinition() == typeof(IHandler<,>))
		{
			typeof(DefaultHandlerContext).GetMethod(nameof(Register), 3, BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string)])
			                             ?.MakeGenericMethod(args.Type, args.Handler.HandlerType.GenericTypeArguments[1], args.Handler.HandlerType)
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
			var handler = provider.GetRequiredService<THandler>();
			return async (message, context, token) => await handler.HandleAsync((TMessage)message, context, token);
		}

		_handlerContainer.GetOrAdd(channel, _ => []).Add(new HandlerRegistration(typeof(THandler).FullName, Handling));
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, typeof(TMessage), typeof(THandler)));
	}

	private void Register(string channel, Type type, object instance, MethodInfo method)
	{
		var invoker = BuildHandlerInvoker(method);
		if (invoker == null)
		{
			_logger.LogWarning("Handler method {Method} on channel {Channel} has more than three parameters and cannot be registered", method.Name, channel);
			return;
		}

		HandlerDelegate Handling(IServiceProvider provider)
		{
			var handler = instance ?? ActivatorUtilities.GetServiceOrCreateInstance(provider, type);

			return (message, context, token) => invoker(handler, message, context, token);
		}

		_handlerContainer.GetOrAdd(channel, _ => []).Add(new HandlerRegistration(type.FullName, Handling));
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
				_logger.LogWarning("Handler method {Method} on channel {Channel} has more than three parameters and cannot be registered", channelHandler.Method.Name, channel);
				return;
			}

			handling = provider =>
			{
				var instance = channelHandler.Instance ?? ActivatorUtilities.GetServiceOrCreateInstance(provider, channelHandler.HandlerType);

				return (message, context, token) => invoker(instance, message, context, token);
			};
		}

		_handlerContainer.GetOrAdd(channel, _ => []).Add(new HandlerRegistration(channelHandler.HandlerType.FullName, handling));
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, null, channelHandler.HandlerType));
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
	public async Task<object> HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(message);

		using var scope = _provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
		if (!_handlerContainer.TryGetValue(channel, out var registrations) || registrations == null || registrations.Count == 0)
		{
			throw new InvalidOperationException($"No handler registered for message {context.MessageId} on channel {channel}");
		}

		// 从服务提供程序获取处理程序实例
		_logger.LogInformation("Message {Id} is being handled", context.MessageId);

		// 收件箱全局开关启用时要求已注册收件箱存储；否则视为配置缺失，快速失败以避免静默退化。
		var useInbox = _inboxOptions.Enabled;
		if (useInbox && _inboxStore == null)
		{
			throw new MessagePersistentException($"The inbox store is not registered, but inbox is enabled. Please register an IInboxStore implementation (e.g. services.AddInMemoryInbox()).");
		}

		object result;

		var handlers = registrations.Select(registration => (Name: registration.Name, Handler: registration.Factory(scope.ServiceProvider))).ToList();

		if (!Convention.IsMulticast(channel, message.GetType()))
		{
			// 单播：执行第一个处理程序；启用收件箱时标记执行结果（不做去重跳过 —— 与 Java 版本保持一致）。
			var (name, handler) = handlers[0];
			if (useInbox)
			{
				try
				{
					result = await handler(message, context, cancellationToken);
					_inboxStore.MarkAsSuccess(context.MessageId, name);
				}
				catch (Exception exception)
				{
					_inboxStore.MarkAsFailed(context.MessageId, name, exception.Message);
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

			if (useInbox && !_inboxStore.Insert(channel, new InboundEnvelope(context, channel, message), names))
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
							_inboxStore.MarkAsSuccess(context.MessageId, handler.Name);
						}
					}
					catch (Exception exception)
					{
						if (useInbox)
						{
							_inboxStore.MarkAsFailed(context.MessageId, handler.Name, exception.Message);
						}

						// 忽略多播处理程序中的错误
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