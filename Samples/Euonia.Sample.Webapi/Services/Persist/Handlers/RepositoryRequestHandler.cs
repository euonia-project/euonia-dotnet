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
/// 仓库读侧（列表）：把行级数据权限下推为查询条件。
/// 已登录用户必须先持有 <c>repository:view</c> 码，否则一视同仁返回空页；
/// 匿名回落默认策略（仅公开行）。排序采用白名单，仅允许按实体列排序。
/// </summary>
internal class RepositoryRequestHandler(IRepositoryStore store, IScopeGuard guard)
	: IHandler<RepositoryListQueryRequest, PagedResult<RepositoryDto>>
{
	public async Task<PagedResult<RepositoryDto>> HandleAsync(RepositoryListQueryRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		var page = message.Page < 1 ? 1 : message.Page;
		var pageSize = message.PageSize < 1 ? 1 : message.PageSize;

		var query = store.Query();

		if (guard.User?.Identity?.IsAuthenticated == true)
		{
			await guard.EnsureResolvedAsync(cancellationToken);
			if (!guard.Permissions.Contains(RepositoryPermissions.View))
			{
				return PagedResult<RepositoryDto>.Empty(page, pageSize);
			}

			query = guard.Apply(query, RepositoryPermissions.View);
		}
		else
		{
			// 匿名：默认策略里 Where(IsPublic) 显式放行公开行，其余（私有/机密）被过滤。
			query = guard.Apply(query);
		}

		if (!string.IsNullOrWhiteSpace(message.Keyword))
		{
			query = query.Where(RepositorySpecification.NameContains(message.Keyword).Satisfy());
		}

		query = ApplySort(query, message.SortBy);

		// OrderBy 必须落在投影之前：EF Core 无法翻译投影后对 DTO 成员的排序，会查询翻译失败。
		var total = await query.CountAsync(cancellationToken);
		var items = await query
		                      .Skip((page - 1) * pageSize)
		                      .Take(pageSize)
		                      .Select(repository => new RepositoryDto(repository.Id, repository.Name, repository.TeamId, repository.OwnerId, repository.Level, repository.IsPublic))
		                      .ToListAsync(cancellationToken);

		return new PagedResult<RepositoryDto>(total, page, pageSize, items);
	}

	private static IQueryable<CodeRepository> ApplySort(IQueryable<CodeRepository> query, string sortBy)
	{
		// 排序白名单：只允许按实体列排序，非法值回落默认（Id）。
		return sortBy?.ToLowerInvariant() switch
		{
			"name" => query.OrderBy(repository => repository.Name).ThenBy(repository => repository.Id),
			"team" or "teamid" => query.OrderBy(repository => repository.TeamId).ThenBy(repository => repository.Id),
			"owner" or "ownerid" => query.OrderBy(repository => repository.OwnerId).ThenBy(repository => repository.Id),
			"level" => query.OrderBy(repository => repository.Level).ThenBy(repository => repository.Id),
			_ => query.OrderBy(repository => repository.Id)
		};
	}
}