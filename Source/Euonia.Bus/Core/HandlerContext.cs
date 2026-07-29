using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Default message handler context using Microsoft dependency injection.
/// </summary>
internal sealed class HandlerContext : IHandlerContext
{
	/// <summary>
	/// Occurs when a message handler is subscribed.
	/// </summary>
	public event EventHandler<MessageSubscribedEventArgs> MessageSubscribed;

	private readonly ConcurrentDictionary<string, List<MessageHandlerFactory>> _handlerContainer = new();
	private readonly IServiceProvider _provider;
	private readonly ILogger<HandlerContext> _logger;
	private readonly IMessageConvention _convention;

	/// <summary>
	/// Initialize a new instance of <see cref="HandlerContext"/>.
	/// </summary>
	/// <param name="provider">The service provider used to resolve handlers, logger and other services.</param>
	public HandlerContext(IServiceProvider provider)
	{
		_provider = provider;
		_logger = provider.GetService<ILoggerFactory>()?.CreateLogger<HandlerContext>() ?? new NullLogger<HandlerContext>();
		_convention = provider.GetService<IMessageBusOptions>()?.Convention ?? new MessageConvention();
	}

	#region Handling register

	/// <summary>
	/// Register a message handler type for the message type <typeparamref name="TMessage"/>.
	/// </summary>
	/// <typeparam name="TMessage">The message type to handle. Must be a reference type.</typeparam>
	/// <typeparam name="TResponse"></typeparam>
	/// <typeparam name="THandler">The handler type that implements <see cref="IHandler{TMessage}"/>.</typeparam>
	internal void Register<TMessage, TResponse, THandler>()
		where TMessage : class
		where THandler : IHandler<TMessage, TResponse>
	{
		var channel = MessageCache.Default.GetOrAddChannel<TMessage>();

		MessageHandler Handling(IServiceProvider provider)
		{
			var handler = provider.GetService<THandler>();
			return (message, context, token) => handler.HandleAsync((TMessage)message, context, token)
			                                           .ContinueWith(task => (object)task.Result, token);
		}

		ConcurrentDictionarySafeRegister(channel, Handling, _handlerContainer);
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(channel, typeof(TMessage), typeof(THandler)));
	}

	/// <summary>
	/// Register a handler described by a <see cref="MessageRegistration"/>.
	/// The registration contains the handler type, the method to invoke and the channel name.
	/// </summary>
	/// <param name="registration">The <see cref="MessageRegistration"/> describing the handler to register.</param>
	internal void Register(MessageRegistration registration)
	{
		MessageHandler Handling(IServiceProvider provider)
		{
			var handler = ActivatorUtilities.GetServiceOrCreateInstance(provider, registration.HandlerType);

			return (message, context, token) =>
			{
				var arguments = GetArguments(registration.Method, message, context, token);
				var expression = MethodInvokerBuilder.BuildCallExpression(handler, registration.Method, arguments);

				return Expression.Lambda<Func<Task<object>>>(expression).Compile()();
			};
		}

		ConcurrentDictionarySafeRegister(registration.Channel, Handling, _handlerContainer);
		MessageSubscribed?.Invoke(this, new MessageSubscribedEventArgs(registration.Channel, registration.MessageType, registration.HandlerType));
	}

	#endregion

	#region Handle message

	/// <inheritdoc />
	public async Task HandleAsync(object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			return;
		}

		var name = MessageCache.Default.GetOrAddChannel(message.GetType());
		await HandleAsync(name, message, context, cancellationToken);
	}

	/// <inheritdoc />
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
				_logger.LogWarning("No handler registered for message {Id} on channel {Channel}", context.MessageId, channel);
				throw new InvalidOperationException($"No handler registered for message {context.MessageId} on channel {channel}");
			}

			// Get handler instance from service provider using Expression Tree
			_logger?.LogInformation("Message {Id} is being handled", context.MessageId);

			if (factories.Count == 1)
			{
				var factory = factories.First();
				await ExecuteHandler(scope, factory, cancellationToken)
				      .AsTask()
				      .ContinueWith(task =>
				      {
					      if (!task.IsCompletedSuccessfully)
					      {
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
					await ExecuteHandler(scope, factory, token);
				});
			}

			_logger!.LogInformation("Message {Id} was completed handled", context.MessageId);
		}

		return;

		async ValueTask<object> ExecuteHandler(IServiceScope scope, MessageHandlerFactory factory, CancellationToken cancellation)
		{
			try
			{
				var handler = factory(scope.ServiceProvider);
				return await handler(message, context, cancellation);
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Error occurred while handling message {Id}", context.MessageId);
				if (_convention.IsRequestType(message.GetType()) || _convention.IsUnicastType(message.GetType()))
				{
					// Swallow the exception for request/response and queue messages
					throw;
				}

				return ValueTask.FromResult(exception);
			}
		}
	}

	#endregion

	#region Supports

	/// <summary>
	/// Safely register a value into a concurrent dictionary whose values are lists.
	/// This method uses a lock on the internal handler container to ensure that list
	/// mutations (add/replace) are thread-safe for the combined key/value operations.
	/// </summary>
	/// <typeparam name="TKey">The dictionary key type.</typeparam>
	/// <typeparam name="TValue">The list element type.</typeparam>
	/// <param name="key">The key to register the value under.</param>
	/// <param name="value">The value to add to the list for the given key.</param>
	/// <param name="registry">The concurrent dictionary that stores lists of values.</param>
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
	/// Build an array of <see cref="Expression"/> arguments for invoking a handler method.
	/// The method supports up to three parameters where parameter positions are resolved by type:
	/// - a parameter matching <see cref="MessageContext"/> will receive the provided <paramref name="context"/> instance.
	/// - a parameter matching <see cref="CancellationToken"/> will receive the provided <paramref name="cancellationToken"/>.
	/// - any other parameter will receive the <paramref name="message"/> instance.
	/// </summary>
	/// <param name="method">The <see cref="MethodInfo"/> representing the handler method to invoke.</param>
	/// <param name="message">The message object to be passed to the handler.</param>
	/// <param name="context">The <see cref="MessageContext"/> to be passed to the handler when requested.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to pass to the handler when requested.</param>
	/// <returns>
	/// An array of <see cref="Expression"/> corresponding to the method parameters, or <c>null</c>
	/// when the method has more than three parameters (unsupported).
	/// </returns>
	private static Expression[] GetArguments(MethodInfo method, object message, MessageContext context, CancellationToken cancellationToken)
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