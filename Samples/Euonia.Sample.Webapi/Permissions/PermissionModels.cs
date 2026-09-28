using Nerosoft.Euonia.Sample.Domain.Permissions;

namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>授权（用户）视图：角色 + 权限码 + 团队范围。</summary>
public sealed record AuthorizationView(string UserId, string Name, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Codes, IReadOnlyCollection<string> Teams)
{
	/// <summary>从授权数据库实体构造视图。</summary>
	public static AuthorizationView From(string userId, DemoAuthorization authorization)
	{
		return new AuthorizationView(userId, authorization.Name, authorization.Roles, authorization.Codes, authorization.Teams);
	}
}

/// <summary>权限变更入参：<see cref="Add"/> 与 <see cref="Remove"/> 均可选，二者同时为空视为无操作。</summary>
public sealed record PermissionChangeInput(IReadOnlyCollection<string> Add, IReadOnlyCollection<string> Remove);