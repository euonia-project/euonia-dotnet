using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 归档命令的数据权限模型：把 <c>project:archive</c> 的行级策略挂在
/// <see cref="ArchiveProjectCommand"/> 自身，而不是 <see cref="Aggregates.Project"/>。
/// </summary>
/// <remarks>
/// <para>
/// 行级策略挂在「资源类型 + 业务操作」上，一个操作一个声明位。Project 聚合的 Update
/// 已经被 <c>project:edit</c> 占用，归档是命令对象上的独立操作，因此它的行级策略必须以
/// 命令对象为资源来声明——命令对象是 <c>ICommandObject</c>，解析出的操作是 Execute，
/// 正好对上它自己的 <c>[Permission(project:archive)]</c>。
/// </para>
/// <para>
/// 维度取值全部来自命令持有的项目实体，因此「行级授予 <c>project:archive|{projectId}</c>」
/// 与聚合侧共用同一套 <c>project</c> 维度，<see cref="ScopeSubjectResolver"/> 无需区分资源类型。
/// </para>
/// </remarks>
public sealed class ArchiveProjectScopeModel : ScopeModel<ArchiveProjectCommand>
{
	/// <inheritdoc/>
	public override void Define(ScopeModelBuilder<ArchiveProjectCommand> builder)
	{
		builder.Map(ScopeDimensions.Owner, command => command.Project.OwnerId)
		       .Map(ProjectScopeModel.ProjectDimension, command => command.Project.Id)
		       .Classify("state", command => command.Project.IsArchived ? "archived" : "active");
	}

	/// <summary>默认策略：仅项目负责人可归档（未持有 archive 码时也按此收敛）。</summary>
	public override ScopePolicy<ArchiveProjectCommand> Policy
		=> ScopePolicy<ArchiveProjectCommand>.Self();

	/// <inheritdoc/>
	public override void Declare(ScopePolicySet<ArchiveProjectCommand> policies)
	{
		// 与编辑 / 删除同构：本人或单行授予放行，但归档项目一律否决（归档即只读凝固）。
		policies.ForOperation(BusinessOperation.Execute,  ProjectScopeModel.Mutable<ArchiveProjectCommand>(command => command.Project.IsArchived),
			ProjectPermissions.Archive);
	}
}