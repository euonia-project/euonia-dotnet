using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 使用 Microsoft 依赖注入的默认消息处理程序上下文。
/// </summary>
internal sealed class DefaultHandlerContext : IHandlerContext
{
	/// <summary>
	/// 当消息处理程序被订阅时触发。
	/// </summary>
	public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed;

	private readonly ConcurrentDictionary<string, List<HandlerFactory>> _handlerContainer = new();
	private readonly IServiceProvider _provider;
	private readonly ILogger<DefaultHandlerContext> _logger;
	private readonly IMessageConvention _convention;

	/// <summary>
	/// 初始化 <see cref="DefaultHandlerContext"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析处理程序、日志记录器和其他服务的服务提供程序。</param>
	public DefaultHandlerContext(IServiceProvider provider)
	{
		_provider = provider;
		_logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultHandlerContext>();
		_convention = provider.GetService<IConfigurator>()?.Convention ?? new BaseMessageConvention();
	}

	#region Handling register

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
			var handler = provider.GetService<THandler>();
			return (message, context, token) => handler.HandleAsync((TMessage)message, context, token)
			                                           .ContinueWith(task => (object)task.Result, token);
		}

		ConcurrentDictionarySafeRegister(channel, Handling, _handlerContainer);
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, typeof(TMessage), typeof(THandler)));
	}

	/// <summary>
	/// 注册由 <see cref="ChannelHandler"/> 描述的处理程序。
	/// 注册信息包含处理程序类型、要调用的方法和通道名称。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="channelHandler">描述要注册的处理程序的 <see cref="ChannelHandler"/> 实例。</param>
	internal void Register(string channel, ChannelHandler channelHandler)
	{
		HandlerDelegate Handling(IServiceProvider provider)
		{
			var instance = channelHandler.Instance ?? ActivatorUtilities.GetServiceOrCreateInstance(provider, channelHandler.HandlerType);

			return (message, context, token) =>
			{
				var arguments = GetArguments(channelHandler.Method, message, context, token);
				var expression = MethodInvokerBuilder.BuildCallExpression(instance, channelHandler.Method, arguments);

				return Expression.Lambda<Func<Task<object>>>(expression).Compile()();
			};
		}

		ConcurrentDictionarySafeRegister(channel, Handling, _handlerContainer);
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
	public async Task HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			return;
		}

		using (var scope = _provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
		{
			if (!_handlerContainer.TryGetValue(channel, out var factories) || factories == null || factories.Count == 0)
			{
				throw new InvalidOperationException($"No handler registered for message {context.MessageId} on channel {channel}");
			}

			// 从服务提供程序获取处理程序实例
			_logger.LogInformation("Message {Id} is being handled", context.MessageId);

			if (!_convention.IsMulticast(channel, message.GetType()))
			{
				var factory = factories[0];
				await ExecuteHandler(scope, factory, cancellationToken).AsTask()
				                                                       .ContinueWith(task =>
				                                                       {
					                                                       if (!task.IsCompletedSuccessfully)
					                                                       {
						                                                       // 对于请求/响应和队列消息，吞掉异常
						                                                       switch (task.Exception)
						                                                       {
							                                                       case null:
								                                                       context.Failure(new InternalServerErrorException());
								                                                       break;
							                                                       case var aggEx when aggEx.InnerExceptions.Count == 1:
								                                                       context.Failure(aggEx.InnerExceptions[0]);
								                                                       break;
							                                                       default:
								                                                       context.Failure(task.Exception.GetBaseException());
								                                                       break;
						                                                       }
					                                                       }
					                                                       //else if (Reflect.TryGetPropertyValue(task, "Result", out var result))
					                                                       else if (task.Result is not Unit)
					                                                       {
						                                                       context.Response(task.Result);
					                                                       }
				                                                       }, cancellationToken);
			}
			else
			{
				await Parallel.ForEachAsync(factories, cancellationToken, async (factory, token) =>
				{
					await ExecuteHandler(scope, factory, token).AsTask()
					                                           .ContinueWith(_ =>
					                                           {
						                                           // 忽略多播处理程序中的错误
					                                           }, token);
				});
			}

			_logger.LogInformation("Message {Id} was completed handled", context.MessageId);
		}

		return;

		async ValueTask<object> ExecuteHandler(IServiceScope scope, HandlerFactory factory, CancellationToken cancellation)
		{
			var handler = factory(scope.ServiceProvider);
			return await handler(message, context, cancellation);
		}
	}

	#endregion

	#region Supports

	/// <summary>
	/// 安全地将值注册到值为列表的并发字典中。
	/// 此方法使用内部处理程序容器上的锁，确保列表变更（添加/替换）在组合的键/值操作中保持线程安全。
	/// </summary>
	/// <typeparam name="TKey">字典键的类型。</typeparam>
	/// <typeparam name="TValue">列表元素的类型。</typeparam>
	/// <param name="key">要注册到的键。</param>
	/// <param name="value">要添加到给定键对应的列表中的值。</param>
	/// <param name="registry">存储值列表的并发字典。</param>
	private void ConcurrentDictionarySafeRegister<TKey, TValue>(TKey key, TValue value, ConcurrentDictionary<TKey, List<TValue>> registry)
	{
		lock (_handlerContainer)
		{
			if (registry.TryGetValue(key, out var handlers))
			{
				if (handlers != null)
				{
					if (!handlers.Contains(value))
					{
						registry[key].Add(value);
					}
				}
				else
				{
					registry[key] = [value];
				}
			}
			else
			{
				registry.TryAdd(key, [value]);
			}
		}
	}

	/// <summary>
	/// 构建用于调用处理程序方法的 <see cref="Expression"/> 参数数组。
	/// 该方法最多支持三个参数，参数位置根据类型解析：
	/// - 匹配 <see cref="MessageContext"/> 类型的参数将接收传入的 <paramref name="context"/> 实例。
	/// - 匹配 <see cref="CancellationToken"/> 类型的参数将接收传入的 <paramref name="cancellationToken"/>。
	/// - 其余任何参数将接收 <paramref name="message"/> 实例。
	/// </summary>
	/// <param name="method">表示要调用的处理程序方法的 <see cref="MethodInfo"/>。</param>
	/// <param name="message">要传递给处理程序的消息对象。</param>
	/// <param name="context">当方法需要时传递给处理程序的 <see cref="MessageContext"/> 实例。</param>
	/// <param name="cancellationToken">当方法需要时传递给处理程序的 <see cref="CancellationToken"/>。</param>
	/// <returns>
	/// 与方法参数对应的 <see cref="Expression"/> 数组；当方法参数超过三个（不支持）时返回 <c>null</c>。
	/// </returns>
	private static Expression[] GetArguments(MethodInfo method, object message, IMessageContext context, CancellationToken cancellationToken)
	{
		var parameterInfos = method.GetParameters();
		var arguments = new Expression[parameterInfos.Length];
		switch (parameterInfos.Length)
		{
			case 0:
				break;
			case 1:
			{
				var parameterType = parameterInfos[0].ParameterType;

				if (parameterType == typeof(MessageContext))
				{
					arguments[0] = Expression.Constant(context);
				}
				else if (parameterType == typeof(CancellationToken))
				{
					arguments[0] = Expression.Constant(cancellationToken);
				}
				else
				{
					arguments[0] = Expression.Constant(message);
				}
			}
				break;
			case 2:
			case 3:
			{
				arguments[0] ??= Expression.Constant(message);

				for (var index = 1; index < parameterInfos.Length; index++)
				{
					if (parameterInfos[index].ParameterType == typeof(MessageContext))
					{
						arguments[index] = Expression.Constant(context);
					}

					if (parameterInfos[index].ParameterType == typeof(CancellationToken))
					{
						arguments[index] = Expression.Constant(cancellationToken);
					}
				}
			}
				break;
			default:
				return null;
		}

		return arguments;
	}

	#endregion
}