using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 把账号移出团队的命令对象。裁决路径与 <see cref="AddTeamMemberCommand"/> 完全一致
/// （同一枚 <see cref="TeamPermissions.Edit"/> 码 + 同一行级策略），只是把成员关系置为失效。
/// </summary>
public sealed class RemoveTeamMemberCommand : CommandObjectBase<RemoveTeamMemberCommand>
{
	/// <summary>目标团队标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string TeamId { get; set; }

	/// <summary>要移出的账号标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
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

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(team, TeamPermissions.Edit))
		{
			throw new SecurityException(guard.ExplainObject(team, TeamPermissions.Edit));
		}

		await store.RemoveMemberAsync(TeamId, UserId, cancellationToken);
	}
}
