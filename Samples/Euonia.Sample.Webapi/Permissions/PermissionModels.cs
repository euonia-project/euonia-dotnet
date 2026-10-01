using Nerosoft.Euonia.Sample.Domain.Permissions;

namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>授权（账号）视图：角色 + 权限码 + 团队范围 + 行级授予。角色来自账号角色表，其余来自授权数据。</summary>
public sealed record AuthorizationView(string UserId, string Name, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Codes, IReadOnlyCollection<string> Teams, IReadOnlyCollection<string> Grants)
{
	/// <summary>由授权数据与账号角色构造视图。</summary>
	public static AuthorizationView From(string userId, string name, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> codes, IReadOnlyCollection<string> teams, IReadOnlyCollection<string> grants)
	{
		return new AuthorizationView(userId, name, roles, codes, teams, grants);
	}
}

/// <summary>权限变更入参：<see cref="Add"/> 与 <see cref="Remove"/> 均可选，二者同时为空视为无操作。</summary>
public sealed record PermissionChangeInput(IReadOnlyCollection<string> Add, IReadOnlyCollection<string> Remove);

/// <summary>账号列表项（账号标识 + 显示名）。</summary>
public sealed record PermissionUserDto(string UserId, string Name);