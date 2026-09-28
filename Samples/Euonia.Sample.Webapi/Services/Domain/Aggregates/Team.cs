using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 团队业务对象，与 <see cref="CodeRepository"/> 同层：团队本身也是受数据权限保护的资源，
/// 可见性按「团队成员或团队负责人」判定（见 <see cref="TeamScopeModel"/>）。
/// 创建/编辑/删除由操作权限与角色（<see cref="PermissionAttribute"/>）闸门控制。
/// </summary>
public sealed class Team : EditableObjectBase<Team, string>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	public static readonly PropertyInfo<string> LeaderIdProperty = RegisterProperty<string>(p => p.LeaderId);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	public string LeaderId
	{
		get => GetProperty(LeaderIdProperty);
		set => SetProperty(LeaderIdProperty, value);
	}

	[Permission(TeamPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryCreate]
	private async Task CreateAsync(string name, CancellationToken cancellationToken = default)
	{
		Name = name;
		LeaderId = BusinessContext.User?.UserId;
		Id = Guid.NewGuid().ToString("N");
		await Task.CompletedTask;
	}

	[Permission(TeamPermissions.View, RoleName.Developer, RoleName.Tester, RoleName.ProjectManager)]
	[FactoryFetch]
	private async Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		var team = await BusinessContext.GetRequiredService<ITeamStore>().GetAsync(id, cancellationToken);
		if (team == null)
		{
			throw new InvalidOperationException($"Team with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, team.Id);
		LoadProperty(NameProperty, team.Name);
		LoadProperty(LeaderIdProperty, team.LeaderId);
	}

	[Permission(TeamPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<ITeamStore>().AddAsync(this, cancellationToken);
	}

	[Permission(TeamPermissions.Edit, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<ITeamStore>().UpdateAsync(this, cancellationToken);
	}

	[Permission(TeamPermissions.Delete, RoleName.ProjectManager)]
	[FactoryDelete]
	private async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var team = await BusinessContext.GetRequiredService<ITeamStore>().GetAsync(id, cancellationToken);
		if (team == null)
		{
			throw new InvalidOperationException($"Team with ID '{id}' not found.");
		}

		// 范围列必须在工厂方法内填充，才能先做行级判定再删除：
		// BusinessObjectFactory 仅在方法返回后做 after 判定，直接删会先删后验（拒了也删了）。
		LoadProperty(IdProperty, team.Id);
		LoadProperty(NameProperty, team.Name);
		LoadProperty(LeaderIdProperty, team.LeaderId);

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(this, TeamPermissions.Delete))
		{
			throw new SecurityException(guard.ExplainObject(this, TeamPermissions.Delete));
		}

		await BusinessContext.GetRequiredService<ITeamStore>().DeleteAsync(id, cancellationToken);
	}
}