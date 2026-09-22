using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus;

/// <inheritdoc cref="IDeadLetterService"/>
internal sealed class DeadLetterService : IDeadLetterService
{
	private static readonly MethodInfo _redeliverMethod
		= typeof(DeadLetterService).GetMethod(nameof(RedeliverAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

	private readonly IServiceAccessor _accessor;
	private readonly IDeadLetterStore _store;

	public DeadLetterService(IServiceAccessor accessor, IDeadLetterStore store)
	{
		_accessor = accessor;
		_store = store;
	}

	/// <inheritdoc/>
	public IReadOnlyList<DeadLetterEntry> GetAll()
	{
		return _store.GetAll();
	}

	/// <inheritdoc/>
	public DeadLetterEntry Get(string messageId)
	{
		return string.IsNullOrWhiteSpace(messageId) ? null : _store.Get(messageId);
	}

	/// <inheritdoc/>
	public async Task<bool> ReplayAsync(string messageId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(messageId))
		{
			return false;
		}

		var entry = _store.Get(messageId);
		if (entry == null)
		{
			return false;
		}

		try
		{
			if (entry.Source == DeadLetterSource.Inbox)
			{
				await ReplayInboxAsync(entry, cancellationToken);
			}
			else
			{
				await ReplayOutboxAsync(entry, cancellationToken);
			}
		}
		catch (Exception exception)
		{
			entry.Error = exception.Message;
			return false;
		}

		_store.Remove(messageId);
		return true;
	}

	/// <inheritdoc/>
	public bool Discard(string messageId)
	{
		return !string.IsNullOrWhiteSpace(messageId) && _store.Remove(messageId);
	}

	/// <summary>
	/// 收件箱重放：直接经由处理程序上下文在原通道上重新处理，无需反射。
	/// </summary>
	private async Task ReplayInboxAsync(DeadLetterEntry entry, CancellationToken cancellationToken)
	{
		var handlerContext = _accessor.GetRequiredService<IHandlerContext>();
		var context = new MessageContext(entry.Content);

		await handlerContext.HandleAsync(entry.Channel, entry.Content.Payload, context, cancellationToken);
	}

	/// <summary>
	/// 发件箱重放：把信封重新交给原传输器投递。
	/// </summary>
	/// <remarks>
	/// <see cref="ITransporter.PublishAsync{TMessage}"/> 是泛型方法，而此处只知道负载的运行时类型，
	/// 因此需要按负载类型构造并调用泛型方法（与 <c>OutboxDispatcher</c> 的重投递路径一致）。
	/// </remarks>
	private Task ReplayOutboxAsync(DeadLetterEntry entry, CancellationToken cancellationToken)
	{
		var payloadType = entry.Content?.Payload?.GetType() ?? typeof(object);
		var method = _redeliverMethod.MakeGenericMethod(payloadType);
		return (Task)method.Invoke(this, [entry, cancellationToken])!;
	}

	private async Task RedeliverAsync<TMessage>(DeadLetterEntry entry, CancellationToken cancellationToken)
	{
		var transporter = _accessor.GetKeyedService<ITransporter>(entry.Target)
		                  ?? throw new MessageTransportException($"The transport '{entry.Target}' is not registered.");

		await transporter.PublishAsync((IMessageEnvelope<TMessage>)entry.Content, cancellationToken);
	}
}
