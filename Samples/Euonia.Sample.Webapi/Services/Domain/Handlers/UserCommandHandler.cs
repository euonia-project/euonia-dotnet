using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Business.Handlers;

internal sealed class UserCommandHandler(IObjectFactory factory, IActuator actuator, IUserRepository users)
	: CommandHandlerBase(factory, actuator),
	  IHandler<UserCreateCommand, string>,
	  IHandler<UserUpdateCommand>
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

	/// <summary>
	/// 更新用户资料：直接操作持久化实体，不经聚合。
	/// 资料字段的写入语义（邮箱小写规范化等）由 <see cref="Persist.Entities.UserEntity"/> 的内部方法承担；
	/// 聚合侧只负责口令等需要跨字段规则的字段。
	/// </summary>
	public async Task HandleAsync(UserUpdateCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		var user = await users.GetAsync(message.Id, true, cancellationToken);
		if (user == null)
		{
			throw new NotFoundException($"User with ID '{message.Id}' not found.");
		}

		user.SetNickname(message.Nickname);
		user.SetEmail(message.Email);
		user.SetPhone(message.Phone);
		await users.UpdateAsync(user, true, cancellationToken);
	}
}