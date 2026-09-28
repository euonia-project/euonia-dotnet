namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 某个演示用户的授权数据：持有的权限码与（权限码无关的）部门范围。
/// 演示授权数据存于授权数据库中；判定时由
/// <see cref="ProjectScopeSubjectResolver"/> 从中实时解析。
/// </summary>
public sealed record ProjectAuthorization(string Name, IReadOnlyCollection<string> Codes, IReadOnlyCollection<string> Depts);

/// <summary>
/// 演示用的授权数据库（进程内数据）。真实系统应以此为授权数据源，
/// 权限码与行级授予都不能固化在令牌中，否则取消授权后旧令牌依然有效。
/// </summary>
public sealed class ProjectAuthorizationStore
{
	private readonly IReadOnlyDictionary<string, ProjectAuthorization> _users;

	public ProjectAuthorizationStore()
	{
		var createProjectCodes = new[] { ProjectPermissions.Create };
		_users = new Dictionary<string, ProjectAuthorization>(StringComparer.Ordinal)
		{
			// 阿一：全量操作权限，能看到 d-1、d-2 两个部门
			["u-1"] = new("阿一", [.. createProjectCodes, ProjectPermissions.View, ProjectPermissions.Edit, ProjectPermissions.Delete], ["d-1", "d-2"]),
			// 阿二：创建 + 查看 + 编辑，只能看到 d-1 部门
			["u-2"] = new("阿二", [.. createProjectCodes, ProjectPermissions.View, ProjectPermissions.Edit], ["d-1"]),
			// 阿三：只能查看，只能看到 d-3 部门
			["u-3"] = new("阿三", [ProjectPermissions.View], ["d-3"]),
		};
	}

	public bool TryGet(string userId, out ProjectAuthorization authorization)
	{
		return _users.TryGetValue(userId, out authorization);
	}
}