using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 代码仓库业务对象。操作权限、角色与行级数据权限在工厂边界强制执行：
/// 每个工厂方法用 <see cref="PermissionAttribute"/> 声明一个权限码与允许的角色：
/// <see cref="RepositoryPermissions.Create"/> / <see cref="RepositoryPermissions.View"/> /
/// <see cref="RepositoryPermissions.Push"/>（push 即本工程的更新操作，见 PERMISSION-SAMPLE.md 场景三）/
/// <see cref="RepositoryPermissions.Delete"/>。push 的命令载体是 <see cref="PushRepositoryCommand"/>，
/// 与工厂更新共用同一行级策略（<c>repository:push</c>）。
/// 行级数据范围列（<see cref="OwnerId"/> / <see cref="TeamId"/> / <see cref="Level"/> /
/// <see cref="IsPublic"/>）由 <see cref="RepositoryScopeModel"/> 按维度映射。
/// </summary>
public sealed class CodeRepository : EditableObjectBase<CodeRepository, string>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	public static readonly PropertyInfo<string> TeamIdProperty = RegisterProperty<string>(p => p.TeamId);
	public static readonly PropertyInfo<string> OwnerIdProperty = RegisterProperty<string>(p => p.OwnerId);
	public static readonly PropertyInfo<string> LevelProperty = RegisterProperty<string>(p => p.Level);
	public static readonly PropertyInfo<bool> IsPublicProperty = RegisterProperty<bool>(p => p.IsPublic);

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

	/// <summary>仓库所有者（使用者标识）。建仓时取当前用户，不可经普通更新路径修改。</summary>
	public string OwnerId
	{
		get => GetProperty(OwnerIdProperty);
		set => SetProperty(OwnerIdProperty, value);
	}

	/// <summary>密级：normal（普通）/ secret（机密，任何用户都不可见、不可改）。</summary>
	public string Level
	{
		get => GetProperty(LevelProperty);
		set => SetProperty(LevelProperty, value);
	}

	/// <summary>是否公开：公开行对匿名访问显式放行（见 <see cref="RepositoryScopeModel"/> 的 <c>Where(IsPublic)</c>）。</summary>
	public bool IsPublic
	{
		get => GetProperty(IsPublicProperty);
		set => SetProperty(IsPublicProperty, value);
	}

	[Permission(RepositoryPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryCreate]
	private async Task CreateAsync(string name, string teamId, CancellationToken cancellationToken = default)
	{
		Name = name;
		TeamId = teamId;
		OwnerId = BusinessContext.User?.UserId;
		Level = RepositoryLevel.Normal;
		IsPublic = false;
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
			throw new NotFoundException($"Repository with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, repository.Id);
		LoadProperty(NameProperty, repository.Name);
		LoadProperty(TeamIdProperty, repository.TeamId);
		LoadProperty(OwnerIdProperty, repository.OwnerId);
		LoadProperty(LevelProperty, repository.Level);
		LoadProperty(IsPublicProperty, repository.IsPublic);
	}

	[Permission(RepositoryPermissions.Create, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IRepositoryStore>().AddAsync(this, cancellationToken);
	}

	[Permission(RepositoryPermissions.Push, RoleName.Developer, RoleName.ProjectManager)]
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
			throw new NotFoundException($"Repository with ID '{id}' not found.");
		}

		// 范围列必须在工厂方法内填充，才能先做行级判定再删除：
		// BusinessObjectFactory 仅在方法返回后做 after 判定，直接删会先删后验（拒了也删了）。
		LoadProperty(IdProperty, repository.Id);
		LoadProperty(NameProperty, repository.Name);
		LoadProperty(TeamIdProperty, repository.TeamId);
		LoadProperty(OwnerIdProperty, repository.OwnerId);
		LoadProperty(LevelProperty, repository.Level);
		LoadProperty(IsPublicProperty, repository.IsPublic);

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(this, RepositoryPermissions.Delete))
		{
			throw new SecurityException(guard.ExplainObject(this, RepositoryPermissions.Delete));
		}

		await BusinessContext.GetRequiredService<IRepositoryStore>().DeleteAsync(id, cancellationToken);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "仓库名不能为空。"));
		Rules.AddRule(new CommonRule.Regular(NameProperty, "^[a-z0-9-]+$", "仓库名只能包含小写字母、数字和连字符。"));
		Rules.AddRule<CodeRepository>(NameProperty, repository => repository.Name != "admin", "该名称被保留。");
		Rules.AddRule<CodeRepository>(LevelProperty, repository => repository.Level is RepositoryLevel.Normal or RepositoryLevel.Secret, "密级只能是 normal 或 secret。");
	}
}

/// <summary>仓库密级的取值。仅 <see cref="CodeRepository.Level"/> 区分授权语义（secret 一律否决）。</summary>
public static class RepositoryLevel
{
	public const string Normal = "normal";

	public const string Secret = "secret";
}