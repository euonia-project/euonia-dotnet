using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Events;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 项目业务对象。操作权限、角色与行级数据权限在工厂边界强制执行：
/// 每个工厂方法用 <see cref="PermissionAttribute"/> 声明一个权限码与允许的角色：
/// <see cref="ProjectPermissions.Create"/> / <see cref="ProjectPermissions.View"/> /
/// <see cref="ProjectPermissions.Edit"/> / <see cref="ProjectPermissions.Delete"/>；
/// 归档（<see cref="ProjectPermissions.Archive"/>）由命令对象 <see cref="Commands.ArchiveProjectCommand"/>
/// 承载，与工厂更新共用「归档即只读」的行级语义。
/// 行级数据范围列（<see cref="OwnerId"/> / <see cref="IsArchived"/>）由
/// <see cref="ProjectScopeModel"/> 按维度映射。
/// </summary>
public sealed class Project : EditableObjectBase<Project, string>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	public static readonly PropertyInfo<string> DescriptionProperty = RegisterProperty<string>(p => p.Description);
	public static readonly PropertyInfo<string> OwnerIdProperty = RegisterProperty<string>(p => p.OwnerId);
	public static readonly PropertyInfo<bool> IsArchivedProperty = RegisterProperty<bool>(p => p.IsArchived);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	public string Description
	{
		get => GetProperty(DescriptionProperty);
		set => SetProperty(DescriptionProperty, value);
	}

	/// <summary>项目负责人（使用者标识）。建项目时取当前用户，不可经普通更新路径修改。</summary>
	public string OwnerId
	{
		get => GetProperty(OwnerIdProperty);
		set => SetProperty(OwnerIdProperty, value);
	}

	/// <summary>是否已归档。归档即只读凝固：仍可查看，但不可再编辑 / 归档 / 删除（见 <see cref="ProjectScopeModel"/>）。</summary>
	public bool IsArchived
	{
		get => GetProperty(IsArchivedProperty);
		set => SetProperty(IsArchivedProperty, value);
	}

	/// <summary>
	/// 归档项目：置归档标记并发布 <see cref="ProjectArchivedEvent"/>。
	/// 事件随本次保存经总线自动分发（<see cref="ProjectStore"/> 保存后触发）。
	/// </summary>
	public void Archive()
	{
		IsArchived = true;
		RaiseEvent(new ProjectArchivedEvent(Id, DateTime.UtcNow));
	}

	[Permission(ProjectPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryCreate]
	private async Task CreateAsync(string name, string description, CancellationToken cancellationToken = default)
	{
		Name = name;
		Description = description;
		OwnerId = BusinessContext.User?.UserId;
		IsArchived = false;
		Id = Guid.NewGuid().ToString("N");
		await Task.CompletedTask;
	}

	[Permission(ProjectPermissions.View, RoleName.Developer, RoleName.Tester, RoleName.ProjectManager)]
	[FactoryFetch]
	private async Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		var project = await BusinessContext.GetRequiredService<IProjectStore>().GetAsync(id, cancellationToken);
		if (project == null)
		{
			throw new NotFoundException($"Project with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, project.Id);
		LoadProperty(NameProperty, project.Name);
		LoadProperty(DescriptionProperty, project.Description);
		LoadProperty(OwnerIdProperty, project.OwnerId);
		LoadProperty(IsArchivedProperty, project.IsArchived);
	}

	[Permission(ProjectPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IProjectStore>().AddAsync(this, cancellationToken);
	}

	[Permission(ProjectPermissions.Edit, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IProjectStore>().UpdateAsync(this, cancellationToken);
	}

	[Permission(ProjectPermissions.Delete, RoleName.ProjectManager)]
	[FactoryDelete]
	private async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var project = await BusinessContext.GetRequiredService<IProjectStore>().GetAsync(id, cancellationToken);
		if (project == null)
		{
			throw new NotFoundException($"Project with ID '{id}' not found.");
		}

		// 范围列必须在工厂方法内填充，才能先做行级判定再删除：
		// BusinessObjectFactory 仅在方法返回后做 after 判定，直接删会先删后验（拒了也删了）。
		LoadProperty(IdProperty, project.Id);
		LoadProperty(NameProperty, project.Name);
		LoadProperty(DescriptionProperty, project.Description);
		LoadProperty(OwnerIdProperty, project.OwnerId);
		LoadProperty(IsArchivedProperty, project.IsArchived);

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(this, ProjectPermissions.Delete))
		{
			throw new SecurityException(guard.ExplainObject(this, ProjectPermissions.Delete));
		}

		await BusinessContext.GetRequiredService<IProjectStore>().DeleteAsync(id, cancellationToken);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "项目名不能为空。"));
	}
}