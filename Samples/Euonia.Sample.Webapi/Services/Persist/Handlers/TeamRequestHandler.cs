using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Sample.Persist.Requests;
using Nerosoft.Euonia.Sample.Persist.Specifications;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Persist.Handlers;

/// <summary>
/// 团队读侧（列表）：把行级数据权限下推为查询条件。
/// 读侧严格化：必须先持有 <c>team:view</c> 码，否则一视同仁返回空页。
/// 团队行级范围为「本团队成员或负责人」（见 <see cref="TeamScopeModel"/>）。
/// </summary>
internal class TeamRequestHandler(ITeamStore store, IScopeGuard guard)
	: IHandler<TeamListQueryRequest, PagedResult<TeamDto>>
{
	public async Task<PagedResult<TeamDto>> HandleAsync(TeamListQueryRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		var page = message.Page < 1 ? 1 : message.Page;
		var pageSize = message.PageSize < 1 ? 1 : message.PageSize;

		await guard.EnsureResolvedAsync(cancellationToken);
		if (!guard.Permissions.Contains(TeamPermissions.View))
		{
			return PagedResult<TeamDto>.Empty(page, pageSize);
		}

		var query = store.Query();
		query = guard.Apply(query, TeamPermissions.View);

		if (!string.IsNullOrWhiteSpace(message.Keyword))
		{
			query = query.Where(TeamSpecification.NameContains(message.Keyword).Satisfy());
		}

		query = ApplySort(query, message.SortBy);

		// OrderBy 必须落在投影之前：EF Core 无法翻译投影后对 DTO 成员的排序，会查询翻译失败。
		var total = await query.CountAsync(cancellationToken);
		var items = await query
		                      .Skip((page - 1) * pageSize)
		                      .Take(pageSize)
		                      .Select(team => new TeamDto(team.Id, team.Name, team.LeaderId))
		                      .ToListAsync(cancellationToken);

		return new PagedResult<TeamDto>(total, page, pageSize, items);
	}

	private static IQueryable<Team> ApplySort(IQueryable<Team> query, string sortBy)
	{
		// 排序白名单：只允许按实体列排序，非法值回落默认（Id）。
		return sortBy?.ToLowerInvariant() switch
		{
			"name" => query.OrderBy(team => team.Name).ThenBy(team => team.Id),
			"leader" or "leaderid" => query.OrderBy(team => team.LeaderId).ThenBy(team => team.Id),
			_ => query.OrderBy(team => team.Id)
		};
	}
}