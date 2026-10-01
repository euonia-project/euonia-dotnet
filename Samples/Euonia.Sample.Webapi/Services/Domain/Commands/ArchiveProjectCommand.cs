using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 归档项目的命令对象。命令体经 <see cref="IObjectFactory.ExecuteAsync{T}"/> 裁决后才执行：
/// <see cref="BusinessOperation.Execute"/> 的类型级闸门（<see cref="ProjectPermissions.Archive"/> 与角色），
/// 行级判定（单行授予 <c>project:archive</c>）在命令体内做，避免「先归档后判定、拒了也归档」。
/// 归档后经 <see cref="ProjectStore"/> 落库，聚合上挂起的 <see cref="Events.ProjectArchivedEvent"/>
/// 随保存经总线自动分发。
/// </summary>
public sealed class ArchiveProjectCommand : CommandObjectBase<ArchiveProjectCommand>
{
	/// <summary>目标项目标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string ProjectId { get; set; }

	/// <summary>
	/// 已取到并待归档的项目实体：行级判定以命令自身为资源、以该实体为判定依据
	/// （见 <see cref="Permissions.ArchiveProjectScopeModel"/>），故必须在判定前装载。
	/// </summary>
	public Project Project { get; private set; }

	/// <summary>命令是否已成功执行。</summary>
	public bool Archived { get; private set; }

	// 装载放在 Create 而非 Execute：工厂在命令体之前先做一次行级判定（EnsureAuthorizedBefore），
	// 那时命令已由工厂方法填充，范围列才有效——这正是框架对「取数后才谈得上数据范围」的约定。
	// 这里直接读仓储而非经工厂取数：归档的授权依据是 project:archive，不应再叠加 project:view 的行级判定。
	[FactoryCreate]
	private async Task CreateAsync(string projectId, CancellationToken cancellationToken = default)
	{
		ProjectId = projectId;
		Project = await BusinessContext.GetRequiredService<IProjectStore>().GetAsync(projectId, cancellationToken);
		if (Project == null)
		{
			throw new NotFoundException($"Project with ID '{projectId}' not found.");
		}
	}

	[Permission(ProjectPermissions.Archive, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryExecute]
	protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		// 行级判定由工厂在执行前完成（资源即命令自身，策略键 project:archive）；
		// 此处兜底再判一次，防止命令被绕过执行器直接调用。
		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(this, ProjectPermissions.Archive))
		{
			throw new SecurityException(guard.ExplainObject(this, ProjectPermissions.Archive));
		}

		Project.Archive();
		await BusinessContext.GetRequiredService<IProjectStore>().UpdateAsync(Project, cancellationToken);
		Archived = true;
	}
}