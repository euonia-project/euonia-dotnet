using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Persist.Repositories;

/// <summary>
/// <see cref="ITeamStore"/> 的 EF Core + SQLite 实现。
/// 仓储只负责存取，权限判定全部由权限引擎完成；读侧下推直接作用于
/// <see cref="Team"/> 的查询条件，越权行在数据库侧被过滤。
/// </summary>
public sealed class TeamStore(IApplicationDataContext context) : ITeamStore
{
	public IQueryable<Team> Query()
	{
		return context.Teams;
	}

	/// <inheritdoc />
	public async Task<Team> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		// 只读实体用于填充工厂新建的聚合实例（LoadProperty），不参与 SaveChanges，
		// 必须 AsNoTracking：否则随后的 UpdateAsync(context.Update(...)) 会因同一主键被跟踪两次
		// 而抛出 identity conflict（一个来自 GetAsync 的 FindAsync，一个来自保存的聚合）。
		return await context.Teams.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<Team> GetWithMembersAsync(string id, CancellationToken cancellationToken = default)
	{
		var team = await GetAsync(id, cancellationToken);

		return team == null ? null : await WithMembersAsync(team, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyCollection<string>> GetTeamIdsAsync(string userId, CancellationToken cancellationToken = default)
	{
		// 只取有效关系：失效的成员关系保留在表里（审计），但不参与判定
		return await context.TeamMembers.AsNoTracking()
		                    .Where(member => member.UserId == userId && member.Status == TeamMemberStatus.Active)
		                    .Select(member => member.TeamId)
		                    .Distinct()
		                    .ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetMembershipsAsync(CancellationToken cancellationToken = default)
	{
		var rows = await context.TeamMembers.AsNoTracking()
		                    .Where(member => member.Status == TeamMemberStatus.Active)
		                    .Select(member => new { member.UserId, member.TeamId })
		                    .ToListAsync(cancellationToken);

		return rows.GroupBy(row => row.UserId, StringComparer.Ordinal)
		           .ToDictionary(
			           group => group.Key,
			           group => (IReadOnlyCollection<string>)group.Select(row => row.TeamId).Distinct().ToArray(),
			           StringComparer.Ordinal);
	}

	/// <inheritdoc />
	public async Task AddMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default)
	{
		var existing = await context.TeamMembers.FirstOrDefaultAsync(member => member.TeamId == teamId && member.UserId == userId, cancellationToken);

		if (existing == null)
		{
			context.TeamMembers.Add(new TeamMember
			{
				Id = Guid.NewGuid().ToString("N"),
				TeamId = teamId,
				UserId = userId,
				Status = TeamMemberStatus.Active
			});
		}
		else
		{
			// 关系以 (团队, 用户) 唯一：重新加入即把失效的关系恢复为有效，而不是再插一行
			existing.Status = TeamMemberStatus.Active;
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task RemoveMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default)
	{
		var existing = await context.TeamMembers.FirstOrDefaultAsync(member => member.TeamId == teamId && member.UserId == userId, cancellationToken);

		if (existing == null)
		{
			return;
		}

		// 置为失效而不是删行：团队成员维度按 status 过滤，因此语义等价于「移出」，
		// 同时留下可审计的痕迹（也顺带演示子表属性如何参与判定）。
		existing.Status = TeamMemberStatus.Expired;
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
	{
		context.Teams.Add(team);
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task UpdateAsync(Team team, CancellationToken cancellationToken = default)
	{
		context.Update(team);
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var team = await context.Teams.FindAsync([id], cancellationToken);
		if (team != null)
		{
			context.Teams.Remove(team);
			await context.SaveChangesAsync(cancellationToken);
		}
	}

	/// <summary>
	/// 填充团队的成员行：<c>team_member</c> 是独立的表，聚合上没有 EF 导航属性，
	/// 因此这里用一次额外的查询把子表行挂到聚合上。
	/// </summary>
	private async Task<Team> WithMembersAsync(Team team, CancellationToken cancellationToken)
	{
		team.Members = await context.TeamMembers.AsNoTracking()
		                           .Where(member => member.TeamId == team.Id)
		                           .ToListAsync(cancellationToken);

		return team;
	}
}