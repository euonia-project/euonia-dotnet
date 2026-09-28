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
}