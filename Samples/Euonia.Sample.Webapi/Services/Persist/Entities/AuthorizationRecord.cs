namespace Nerosoft.Euonia.Sample.Persist.Entities;

/// <summary>授权记录的维度名。</summary>
public static class AuthorizationKinds
{
	/// <summary>权限码。</summary>
	public const string Code = "code";

	/// <summary>所属团队。</summary>
	public const string Team = "team";

	/// <summary>行级授予（<see cref="Nerosoft.Euonia.Sample.Domain.Permissions.RepositoryGrant"/> 编码：操作码与仓库 id 的拼接值）。</summary>
	public const string Grant = "grant";

	/// <summary>显示名。</summary>
	public const string Name = "name";
}

/// <summary>
/// 授权数据表中的一行：某用户在某个维度（权限码 / 所属团队 / 显示名）上的一个取值。
/// 组合主键为 <c>(UserId, Kind, Value)</c>。角色不属于授权维度，由账号角色表（user_role）承载。
/// </summary>
public sealed class AuthorizationRecord
{
	/// <summary>账号标识（对应 <c>user.id</c>）。</summary>
	public string UserId { get; set; }

	/// <summary>维度名，见 <see cref="AuthorizationKinds"/>。</summary>
	public string Kind { get; set; }

	/// <summary>维度取值（如角色名、权限码、团队 id 或显示名）。</summary>
	public string Value { get; set; }
}