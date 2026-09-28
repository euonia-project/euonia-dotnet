using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;

namespace Nerosoft.Euonia.Sample.Business.Handlers;

/// <summary>
/// 团队的命令侧处理：经 <see cref="Actuator"/> 把命令映射为工厂边界操作。
/// 团队编辑/删除仍按负责人行级判定（见 <see cref="Permissions.TeamScopeModel"/> 的 owner 策略）。
/// </summary>
internal sealed class TeamCommandHandler(IObjectFactory factory, IActuator actuator)
	: CommandHandlerBase(factory, actuator),
	  IHandler<CreateTeamCommand, string>,
	  IHandler<UpdateTeamCommand>,
	  IHandler<DeleteTeamCommand>
{
	public Task<string> HandleAsync(CreateTeamCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<Team>()
		               .Create(message.Name, cancellationToken)
		               .ExecuteAsync(cancellationToken)
		               .ReturnAsync(target => target.Id);
	}

	public Task HandleAsync(UpdateTeamCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<Team>()
		               .Update(message.Id, cancellationToken)
		               .Handle(team => team.Name = message.Name)
		               .ExecuteAsync(cancellationToken);
	}

	public Task HandleAsync(DeleteTeamCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		// 删除走工厂直接删除：工厂的保存路径（EditableObject.SaveAsync 的 Delete 操作）解析的是
		// DeleteAsync(CancellationToken) 形态的工厂方法，而本聚合的删除方法签名必须带 id；
		// 故用工厂的 DeleteAsync(id, ct) 形态（与旧控制器一致，删除方法内自行判定负责人行级权限）。
		return Factory.DeleteAsync<Team>(message.Id, cancellationToken);
	}
}