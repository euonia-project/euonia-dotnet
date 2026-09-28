using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 代码仓库业务对象，演示操作权限、角色与行级数据权限在工厂边界的强制执行。
/// 每个工厂方法用 <see cref="PermissionAttribute"/> 声明一个权限码与允许的角色：
/// <see cref="RepositoryPermissions.Create"/> / <see cref="RepositoryPermissions.View"/> /
/// <see cref="RepositoryPermissions.Edit"/> / <see cref="RepositoryPermissions.Delete"/>。
/// 行级数据范围列（<see cref="TeamId"/>）由 <see cref="RepositoryScopeModel"/> 映射到团队维度。
/// </summary>
public sealed class CodeRepository : EditableObjectBase<CodeRepository, string>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	public static readonly PropertyInfo<string> TeamIdProperty = RegisterProperty<string>(p => p.TeamId);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	public string TeamId
	{
		get => GetProperty(TeamIdProperty);
		set => SetProperty(TeamIdProperty, value);
	}

	[Permission(RepositoryPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryCreate]
	private async Task CreateAsync(string name, string teamId, CancellationToken cancellationToken = default)
	{
		Name = name;
		TeamId = teamId;
		Id = Guid.NewGuid().ToString("N");
		await Task.CompletedTask;
	}

	[Permission(RepositoryPermissions.View, RoleName.Developer, RoleName.Tester, RoleName.ProjectManager)]
	[FactoryFetch]
	private async Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		var repository = await BusinessContext.GetRequiredService<IRepositoryStore>().GetAsync(id, cancellationToken);
		if (repository == null)
		{
			throw new InvalidOperationException($"Repository with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, repository.Id);
		LoadProperty(NameProperty, repository.Name);
		LoadProperty(TeamIdProperty, repository.TeamId);
	}

	[Permission(RepositoryPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IRepositoryStore>().AddAsync(this, cancellationToken);
	}

	[Permission(RepositoryPermissions.Edit, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IRepositoryStore>().UpdateAsync(this, cancellationToken);
	}

	[Permission(RepositoryPermissions.Delete, RoleName.ProjectManager)]
	[FactoryDelete]
	private async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var repository = await BusinessContext.GetRequiredService<IRepositoryStore>().GetAsync(id, cancellationToken);
		if (repository == null)
		{
			throw new InvalidOperationException($"Repository with ID '{id}' not found.");
		}

		// 范围列必须在工厂方法内填充，才能先做行级判定再删除：
		// BusinessObjectFactory 仅在方法返回后做 after 判定，直接删会先删后验（拒了也删了）。
		LoadProperty(IdProperty, repository.Id);
		LoadProperty(NameProperty, repository.Name);
		LoadProperty(TeamIdProperty, repository.TeamId);

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(this, RepositoryPermissions.Delete))
		{
			throw new SecurityException(guard.ExplainObject(this, RepositoryPermissions.Delete));
		}

		await BusinessContext.GetRequiredService<IRepositoryStore>().DeleteAsync(id, cancellationToken);
	}
}