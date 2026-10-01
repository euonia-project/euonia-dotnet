using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;

namespace Nerosoft.Euonia.Sample.Business.Handlers;

/// <summary>
/// 项目的命令侧处理：经 <see cref="Actuator"/> 把命令映射为工厂边界操作，
/// 操作权限（<see cref="Permissions.ProjectPermissions"/>）、角色与行级数据权限在工厂边界强制执行。
/// 归档不在此处——它是命令对象（<see cref="ArchiveProjectCommand"/>），由应用服务经工厂执行。
/// </summary>
internal sealed class ProjectCommandHandler(IObjectFactory factory, IActuator actuator)
	: CommandHandlerBase(factory, actuator),
	  IHandler<CreateProjectCommand, string>,
	  IHandler<UpdateProjectCommand>,
	  IHandler<DeleteProjectCommand>
{
	public Task<string> HandleAsync(CreateProjectCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<Project>()
		               .Create(message.Name, message.Description, cancellationToken)
		               .ExecuteAsync(cancellationToken)
		               .ReturnAsync(target => target.Id);
	}

	public Task HandleAsync(UpdateProjectCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Actuator.For<Project>()
		               .Update(message.Id, cancellationToken)
		               .Handle(project =>
		               {
				               project.Name = message.Name;
				               if (message.Description != null)
				               {
					               project.Description = message.Description;
				               }
		               })
		               .ExecuteAsync(cancellationToken);
	}

	public Task HandleAsync(DeleteProjectCommand message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		// 删除走工厂直接删除：工厂的保存路径（EditableObject.SaveAsync 的 Delete 操作）解析的是
		// DeleteAsync(CancellationToken) 形态的工厂方法，而本聚合的删除方法需自行承载范围列与行级判定，
		// 签名必须带 id；故用工厂的 DeleteAsync(id, ct) 形态。
		return Factory.DeleteAsync<Project>(message.Id, cancellationToken);
	}
}