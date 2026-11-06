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
public sealed class RemoveTeamMemberCommand : ServiceObjectBase<RemoveTeamMemberCommand>
{
	/// <summary>目标团队标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string TeamId { get; set; } = null!;

	/// <summary>要移出的账号标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string UserId { get; set; } = null!;

	[FactoryCreate]
	private Task CreateAsync(string teamId, string userId, CancellationToken cancellationToken = default)
	{
		TeamId = teamId;
		UserId = userId;
		return Task.CompletedTask;
	}

	/// <summary>
	/// 执行移出团队成员：先取目标团队并做行级判定，通过后把成员关系置为失效。
	/// </summary>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	/// <exception cref="NotFoundException">目标团队不存在时抛出。</exception>
	/// <exception cref="SecurityException">当前用户不是该团队负责人、行级判定未通过时抛出。</exception>
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
