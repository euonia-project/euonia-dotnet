using Nerosoft.Euonia.Bus.Tests.Requests;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

public class UserExceptionRequestHandler : IHandler<UserExceptionRequest, int>
{
	public Task<int> HandleAsync(UserExceptionRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Task.FromException<int>(new NotFoundException("User not found"));
	}
}

