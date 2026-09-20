using System.Collections.Concurrent;
using Nerosoft.Euonia.Bus.Tests.Events;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

public class UserEventListener
{
	public static readonly ConcurrentBag<UserCreatedEvent> Received = new();

	[Subscribe("user.created")]
	public Task HandleAsync(UserCreatedEvent message, IMessageContext messageContext, CancellationToken cancellationToken = default)
	{
		Received.Add(message);
		return Task.CompletedTask;
	}
}