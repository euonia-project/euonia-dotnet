using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 项目业务对象，演示操作权限与数据权限在工厂边界的强制执行。
/// 每个工厂方法用 <see cref="PermissionAttribute"/> 声明一个权限码：
/// <see cref="ProjectPermissions.Create"/> / <see cref="ProjectPermissions.View"/> /
/// <see cref="ProjectPermissions.Edit"/>。数据范围列（<see cref="OwnerId"/>、<see cref="DeptId"/>）
/// 由 <see cref="ProjectScopeModel"/> 映射。
/// </summary>
public sealed class Project : EditableObjectBase<Project, string>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	public static readonly PropertyInfo<string> OwnerIdProperty = RegisterProperty<string>(p => p.OwnerId);
	public static readonly PropertyInfo<string> DeptIdProperty = RegisterProperty<string>(p => p.DeptId);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	public string OwnerId
	{
		get => GetProperty(OwnerIdProperty);
		set => SetProperty(OwnerIdProperty, value);
	}

	public string DeptId
	{
		get => GetProperty(DeptIdProperty);
		set => SetProperty(DeptIdProperty, value);
	}

	[Permission(ProjectPermissions.Create)]
	[FactoryCreate]
	private async Task CreateAsync(string name, string deptId, CancellationToken cancellationToken = default)
	{
		Name = name;
		DeptId = deptId;
		OwnerId = BusinessContext.User?.UserId;
		Id = Guid.NewGuid().ToString("N");
		await Task.CompletedTask;
	}

	[Permission(ProjectPermissions.View)]
	[FactoryFetch]
	private async Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		var project = await BusinessContext.GetRequiredService<IProjectStore>().GetAsync(id, cancellationToken);
		if (project == null)
		{
			throw new InvalidOperationException($"Project with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, project.Id);
		LoadProperty(NameProperty, project.Name);
		LoadProperty(OwnerIdProperty, project.OwnerId);
		LoadProperty(DeptIdProperty, project.DeptId);
	}

	[Permission(ProjectPermissions.Create)]
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IProjectStore>().AddAsync(this, cancellationToken);
	}

	[Permission(ProjectPermissions.Edit)]
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await BusinessContext.GetRequiredService<IProjectStore>().UpdateAsync(this, cancellationToken);
	}
}