namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 团队成员关系（子表行，对应 <c>team_member(team_id, user_id, status)</c>）。
/// <para>
/// 「谁属于这个团队」是<b>业务数据本身</b>，不是授权数据的副本：团队的可见性直接由这张表实时判定
/// （见 <see cref="Permissions.TeamScopeModel"/> 的成员维度），因此成员关系的增删改就是授权变更，
/// 其写入口必须由操作权限把守（见 <see cref="Commands.AddTeamMemberCommand"/>）。
/// </para>
/// </summary>
public sealed class TeamMember
{
	/// <summary>关系标识。</summary>
	public string Id { get; set; }

	/// <summary>所属团队（<c>team.id</c>）。</summary>
	public string TeamId { get; set; }

	/// <summary>成员（账号标识，<c>user.id</c>）。</summary>
	public string UserId { get; set; }

	/// <summary>关系状态，见 <see cref="TeamMemberStatus"/>。</summary>
	public string Status { get; set; }
}

/// <summary>成员关系状态。失效的关系保留在表里（审计），但不参与判定。</summary>
public static class TeamMemberStatus
{
	/// <summary>有效成员。</summary>
	public const string Active = "active";

	/// <summary>已失效（移出团队）。</summary>
	public const string Expired = "expired";
}
