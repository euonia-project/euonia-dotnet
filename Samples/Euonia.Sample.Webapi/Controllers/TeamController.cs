using System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 团队资源的权限控制演示端点：与 <see cref="RepositoryController"/> 相同的边界强制方式。
/// 团队行级范围为「团队成员或负责人」；编辑/删除仅负责人（按码声明的 owner 策略）。
/// 列表读侧严格化：已登录用户必须持有 <c>team:view</c> 码，否则返回空页。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TeamController(IObjectFactory factory, IScopeGuard guard, ITeamStore store) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> List([FromQuery] string keyword = null, [FromQuery] string sortBy = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
	{
		await guard.EnsureResolvedAsync(cancellationToken);

		// 读侧严格化：必须先持有 team:view 码，否则一视同仁返回空。
		if (!guard.Permissions.Contains(TeamPermissions.View))
		{
			return Ok(PagedResult<TeamDto>.Empty(page, pageSize));
		}

		var query = store.Query();
		query = guard.Apply(query, TeamPermissions.View);
		if (!string.IsNullOrWhiteSpace(keyword))
		{
			query = query.Where(team => team.Name.Contains(keyword));
		}

		// OrderBy 必须落在投影之前：EF Core 无法翻译投影后对 DTO 成员的排序，会查询翻译失败。
		var total = await query.CountAsync(cancellationToken);
		var items = await OrderByTeamSort(query, sortBy)
		                      .Skip((page - 1) * pageSize)
		                      .Take(pageSize)
		                      .Select(team => new TeamDto(team.Id, team.Name, team.LeaderId))
		                      .ToListAsync(cancellationToken);

		return Ok(new PagedResult<TeamDto>(total, page, pageSize, items));
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
		try
		{
			var team = await factory.FetchAsync<Team>(id);
			return Ok(ToDto(team));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	[HttpPost]
	public async Task<IActionResult> CreateAsync([FromBody] TeamCreateInput input, CancellationToken cancellationToken)
	{
		try
		{
			var team = await factory.CreateAsync<Team>(input.Name);
			await team.SaveAsync(forceUpdate: true, cancellationToken: cancellationToken);
			return Ok(ToDto(team));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateAsync(string id, [FromBody] TeamUpdateInput input, CancellationToken cancellationToken)
	{
		try
		{
			var team = await factory.FetchAsync<Team>(id);
			team.Name = input.Name;
			await team.SaveAsync(forceUpdate: true, cancellationToken: cancellationToken);
			return Ok(ToDto(team));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
	{
		try
		{
			await factory.DeleteAsync<Team>(id);
			return Ok(new { id });
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	private static IQueryable<Team> OrderByTeamSort(IQueryable<Team> query, string sortBy)
	{
		return sortBy?.ToLowerInvariant() switch
		{
			"name" => query.OrderBy(team => team.Name).ThenBy(team => team.Id),
			"leader" or "leaderid" => query.OrderBy(team => team.LeaderId).ThenBy(team => team.Id),
			_ => query.OrderBy(team => team.Id)
		};
	}

	private static TeamDto ToDto(Team team)
	{
		return new TeamDto(team.Id, team.Name, team.LeaderId);
	}

	private IActionResult Denied(SecurityException exception)
	{
		return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
	}
}