using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Sample.Business.Handlers;

internal sealed class UserCommandHandler(IUnitOfWorkManager unitOfWork, IObjectFactory factory)
	: CommandHandlerBase(unitOfWork, factory),
	  IHandler<UserCreateCommand>
{
	public Task HandleAsync(UserCreateCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return ExecuteAsync(async () =>
		{
			var business = await Factory.CreateAsync<User>(message.Username, cancellationToken);
			business.Nickname = message.Nickname;
			business.Email = message.Email;
			business.Phone = message.Phone;
			business.SetPassword(message.Password);
			business.MarkAsNew();
			await business.SaveAsync(false, cancellationToken);
			return business.Id;
		}, context.Response, cancellationToken);
	}
}