using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;

namespace Nerosoft.Euonia.Sample.Business.Handlers;

internal sealed class UserCommandHandler(IObjectFactory factory, IActuator actuator)
	: CommandHandlerBase(factory, actuator),
	  IHandler<UserCreateCommand,string>
{
	public Task<string> HandleAsync(UserCreateCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<User>()
		               .Create(message.Username, cancellationToken)
		               .Handle(business =>
		               {
			               business.Nickname = message.Nickname;
			               business.Email = message.Email;
			               business.Phone = message.Phone;
			               business.SetPassword(message.Password);
		               })
		               .ExecuteAsync(cancellationToken)
		               .ReturnAsync(target => target.Id);
		//.NextAsync(context.Response);
	}
}