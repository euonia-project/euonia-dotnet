using System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 权限控制演示端点：
/// <list type="number">
/// <item><description>POST <c>api/auth/login</c>（<see cref="AuthController"/>）签发带身份与角色的令牌。</description></item>
/// <item><description>GET <c>api/repository</c> 用 <see cref="IScopeGuard.Apply{T}"/> 把行级数据权限下推为查询条件
/// （已登录按 <c>repository:view</c> 码与行范围；匿名回落到默认策略，仅公开行）。</description></item>
/// <item><description>POST/GET/PUT/DELETE 走 <see cref="IObjectFactory"/>，操作权限（含角色）与行级数据权限在工厂边界强制执行。</description></item>
/// <item><description>POST <c>api/repository/{id}/push</c> 走命令对象 <see cref="PushRepositoryCommand"/>，行级推送权限在命令体内判定。</description></item>
/// </list>
/// 演示角色：开发（建仓 + 推送被授予的行）、测试（只读可见）、项目管理（推送/删除被授予的行）。
/// 仓库的行级范围按 view（本人/本团队/单行授予，机密除外）与 push/delete（仅单行授予，机密除外）区分；
/// 更新路径与 push 共用 <c>repository:push</c> 码（对齐 SAMPLE.md 场景三）。
/// 演示数据：u-1 开发归属 T-1/T-2；u-2 测试归属 T-1；u-3 项目管理归属 T-3。
/// r-101 公开（匿名可见）；r-102 机密（任何用户都不可见、不可操作）。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RepositoryController(IObjectFactory factory, IScopeGuard guard, IRepositoryStore store) : ControllerBase
{
	[AllowAnonymous]
	[HttpGet]
	public async Task<IActionResult> List([FromQuery] string keyword = null, [FromQuery] string sortBy = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
	{
		await guard.EnsureResolvedAsync(cancellationToken);

		var query = store.Query();

		// 读侧严格化：已登录用户必须先持有 repository:view 码，否则一视同仁返回空；
		// 有码时行范围按 view 码对应的策略（本人/本团队/单行授予，机密除外）下推。
		if (User.Identity?.IsAuthenticated == true)
		{
			if (!guard.Permissions.Contains(RepositoryPermissions.View))
			{
				return Ok(PagedResult<RepositoryDto>.Empty(page, pageSize));
			}

			query = guard.Apply(query, RepositoryPermissions.View);
		}
		else
		{
			// 匿名：默认策略里 Where(IsPublic) 显式放行公开行，其余（私有/机密）被过滤。
			query = guard.Apply(query);
		}

		query = ApplySearch(query, keyword);
		query = ApplySort(query, sortBy);

		// OrderBy 必须落在投影之前：EF Core 无法翻译投影后对 DTO 成员的排序，会查询翻译失败。
		// 排序已由 ApplySort 施加（白名单 + 默认 Id），此处直接分页投影。
		var total = await query.CountAsync(cancellationToken);
		var items = await query
		                      .Skip((page - 1) * pageSize)
		                      .Take(pageSize)
		                      .Select(repository => new RepositoryDto(repository.Id, repository.Name, repository.TeamId, repository.OwnerId, repository.Level, repository.IsPublic))
		                      .ToListAsync(cancellationToken);

		return Ok(new PagedResult<RepositoryDto>(total, page, pageSize, items));
	}

	[AllowAnonymous]
	[HttpGet("{id}")]
	public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			// 匿名：不走工厂（无码无主体），用读模型 + 默认策略判定；公开行放行，私有/机密 403。
			var entity = await store.GetAsync(id, cancellationToken);
			if (entity == null)
			{
				return NotFound();
			}

			if (!guard.Allows(entity))
			{
				return Denied(new SecurityException("当前用户没有权限访问该仓库。"));
			}

			return Ok(ToDto(entity));
		}

		try
		{
			var repository = await factory.FetchAsync<CodeRepository>(id);
			return Ok(ToDto(repository));
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
	public async Task<IActionResult> CreateAsync([FromBody] RepositoryCreateInput input, CancellationToken cancellationToken)
	{
		try
		{
			var repository = await factory.CreateAsync<CodeRepository>(input.Name, input.TeamId);
			if (!string.IsNullOrWhiteSpace(input.Level))
			{
				repository.Level = input.Level;
			}

			if (input.IsPublic.HasValue)
			{
				repository.IsPublic = input.IsPublic.Value;
			}

			await repository.SaveAsync(forceUpdate: true, cancellationToken: cancellationToken);
			return Ok(ToDto(repository));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateAsync(string id, [FromBody] RepositoryUpdateInput input, CancellationToken cancellationToken)
	{
		try
		{
			var repository = await factory.FetchAsync<CodeRepository>(id);
			repository.Name = input.Name;
			if (!string.IsNullOrWhiteSpace(input.Level))
			{
				repository.Level = input.Level;
			}

			if (input.IsPublic.HasValue)
			{
				repository.IsPublic = input.IsPublic.Value;
			}

			await repository.SaveAsync(forceUpdate: true, cancellationToken: cancellationToken);
			return Ok(ToDto(repository));
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
			await factory.DeleteAsync<CodeRepository>(id);
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

	[HttpPost("{id}/push")]
	public async Task<IActionResult> PushAsync(string id, CancellationToken cancellationToken)
	{
		try
		{
			var command = await factory.CreateAsync<PushRepositoryCommand>(id, cancellationToken);
			await factory.ExecuteAsync(command, cancellationToken);
			return Ok(new { id, pushed = command.Pushed });
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

	[HttpGet("{id}/explain")]
	public async Task<IActionResult> ExplainAsync(string id, CancellationToken cancellationToken)
	{
		try
		{
			var repository = await factory.FetchAsync<CodeRepository>(id);
			return Ok(guard.Explain(repository));
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

	private static IQueryable<CodeRepository> ApplySearch(IQueryable<CodeRepository> query, string keyword)
	{
		if (string.IsNullOrWhiteSpace(keyword))
		{
			return query;
		}

		return query.Where(repository => repository.Name.Contains(keyword));
	}

	private static IQueryable<CodeRepository> ApplySort(IQueryable<CodeRepository> query, string sortBy)
	{
		// 排序白名单：只允许按实体列排序（OrderBy 必须早于投影），非法值回落默认（Id）。
		return sortBy?.ToLowerInvariant() switch
		{
			"name" => query.OrderBy(repository => repository.Name).ThenBy(repository => repository.Id),
			"team" or "teamid" => query.OrderBy(repository => repository.TeamId).ThenBy(repository => repository.Id),
			"owner" or "ownerid" => query.OrderBy(repository => repository.OwnerId).ThenBy(repository => repository.Id),
			"level" => query.OrderBy(repository => repository.Level).ThenBy(repository => repository.Id),
			_ => query.OrderBy(repository => repository.Id)
		};
	}

	private static RepositoryDto ToDto(CodeRepository repository)
	{
		return new RepositoryDto(repository.Id, repository.Name, repository.TeamId, repository.OwnerId, repository.Level, repository.IsPublic);
	}

	private IActionResult Denied(SecurityException exception)
	{
		return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
	}
}