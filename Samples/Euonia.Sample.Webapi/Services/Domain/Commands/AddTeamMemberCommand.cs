using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 把账号加入团队的命令对象。命令体经 <see cref="IObjectFactory.ExecuteAsync{T}"/> 裁决后才执行：
/// <see cref="TeamPermissions.Edit"/> 的类型级闸门（与角色）先过，行级判定（仅本团队负责人）
/// 在命令体内做，避免「先加成员后判定、拒了也加进去了」。
/// </summary>
/// <remarks>
/// 成员表的写入口<b>就是授权面</b>：团队可见性由 <c>team_member</c> 实时判定，
/// 因此「给自己加一行」等于给自己授权，这条路径必须由操作权限把守
/// （见 Euonia.Security/README.md §5.9 的边界表）。
/// </remarks>
public sealed class AddTeamMemberCommand : CommandObjectBase<AddTeamMemberCommand>
{
	/// <summary>目标团队标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string TeamId { get; set; }

	/// <summary>要加入的账号标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string UserId { get; set; }

	[FactoryCreate]
	private Task CreateAsync(string teamId, string userId, CancellationToken cancellationToken = default)
	{
		TeamId = teamId;
		UserId = userId;
		return Task.CompletedTask;
	}

	[Permission(TeamPermissions.Edit, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryExecute]
	protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var store = BusinessContext.GetRequiredService<ITeamStore>();

		var team = await store.GetAsync(TeamId, cancellationToken);
		if (team == null)
		{
			throw new NotFoundException($"Team with ID '{TeamId}' not found.");
		}

		// 行级判定：只有本团队负责人能改成员表。编辑策略只用行内的列（负责人），
		// 因此这里不需要加载成员（子表）——单行判定的对象图要求只针对「策略引用到的」子表维度。
		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(team, TeamPermissions.Edit))
		{
			throw new SecurityException(guard.ExplainObject(team, TeamPermissions.Edit));
		}

		await store.AddMemberAsync(TeamId, UserId, cancellationToken);
	}
}
