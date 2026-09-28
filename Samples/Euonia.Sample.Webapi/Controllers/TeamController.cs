using System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 团队资源的权限控制演示端点：与 <see cref="RepositoryController"/> 相同的边界强制方式。
/// 团队行级范围为「团队成员或负责人」；编辑/删除仅负责人（按码声明的 owner 策略）。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TeamController(IObjectFactory factory, IScopeGuard guard, ITeamStore store) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> List(CancellationToken cancellationToken)
	{
		// 读侧：把数据权限下推为查询条件，越权行在数据库侧被过滤。
		// OrderBy 必须落在投影之前：EF Core 无法翻译投影后对 DTO 成员的排序，会查询翻译失败。
		var teams = await guard.Apply(store.Query())
		                       .OrderBy(team => team.Id)
		                       .Select(team => new TeamDto(team.Id, team.Name, team.LeaderId))
		                       .ToListAsync(cancellationToken);
		return Ok(teams);
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

	private static TeamDto ToDto(Team team)
	{
		return new TeamDto(team.Id, team.Name, team.LeaderId);
	}

	private IActionResult Denied(SecurityException exception)
	{
		return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
	}
}