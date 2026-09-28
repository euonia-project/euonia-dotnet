using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;

namespace Nerosoft.Euonia.Sample.Business.Handlers;

/// <summary>
/// 代码仓库的命令侧处理：经 <see cref="Actuator"/> 把命令映射为工厂边界操作，
/// 操作权限（<see cref="Permissions.RepositoryPermissions"/>）、角色与行级数据权限在工厂边界强制执行。
/// </summary>
internal sealed class RepositoryCommandHandler(IObjectFactory factory, IActuator actuator)
	: CommandHandlerBase(factory, actuator),
	  IHandler<CreateRepositoryCommand, string>,
	  IHandler<UpdateRepositoryCommand>,
	  IHandler<DeleteRepositoryCommand>
{
	public Task<string> HandleAsync(CreateRepositoryCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<CodeRepository>()
		               .Create(message.Name, message.TeamId, cancellationToken)
		               .Handle(repository =>
		               {
			               if (!string.IsNullOrWhiteSpace(message.Level))
			               {
				               repository.Level = message.Level;
			               }

			               if (message.IsPublic.HasValue)
			               {
				               repository.IsPublic = message.IsPublic.Value;
			               }
		               })
		               .ExecuteAsync(cancellationToken)
		               .ReturnAsync(target => target.Id);
	}

	public Task HandleAsync(UpdateRepositoryCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<CodeRepository>()
		               .Update(message.Id, cancellationToken)
		               .Handle(repository =>
		               {
			               repository.Name = message.Name;
			               if (!string.IsNullOrWhiteSpace(message.Level))
			               {
				               repository.Level = message.Level;
			               }

			               if (message.IsPublic.HasValue)
			               {
				               repository.IsPublic = message.IsPublic.Value;
			               }
		               })
		               .ExecuteAsync(cancellationToken);
	}

	public Task HandleAsync(DeleteRepositoryCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		// 删除走工厂直接删除：工厂的保存路径（EditableObject.SaveAsync 的 Delete 操作）解析的是
		// DeleteAsync(CancellationToken) 形态的工厂方法，而本聚合的删除方法需自行承载范围列与行级判定，
		// 签名必须带 id；故用工厂的 DeleteAsync(id, ct) 形态（与旧控制器一致）。
		return Factory.DeleteAsync<CodeRepository>(message.Id, cancellationToken);
	}
}