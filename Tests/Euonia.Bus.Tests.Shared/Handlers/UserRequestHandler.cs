using Nerosoft.Euonia.Bus.Tests.Requests;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

public class UserRequestHandler : IHandler<UserCountRequest, int>
{
	public async Task<int> HandleAsync(UserCountRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return 0;
	}
}