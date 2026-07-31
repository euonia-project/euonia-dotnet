using Nerosoft.Euonia.Bus.Tests.Commands;

namespace Nerosoft.Euonia.Bus.Tests.Handlers
{
	public class UserCommandHandler : IHandler<UserCreateCommand, int>, IHandler<UserUpdateCommand>
	{
		public bool CanHandle(Type messageType)
		{
			return true;
		}

		public Task<int> HandleAsync(UserCreateCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
		{
			Console.WriteLine("UserCreateCommand handled");

			return Task.FromResult(1);
		}

		public async Task HandleAsync(UserUpdateCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
		{
			Console.WriteLine("UserUpdateCommand handled");
			await Task.CompletedTask;
		}
	}
}