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
/// 权限控制演示端点：
/// <list type="number">
/// <item><description>POST <c>api/auth/login</c>（<see cref="AuthController"/>）签发带身份与角色的令牌。</description></item>
/// <item><description>GET <c>api/repository</c> 用 <see cref="IScopeGuard.Apply{T}"/> 把团队数据权限下推为查询条件。</description></item>
/// <item><description>POST/GET/PUT/DELETE 走 <see cref="IObjectFactory"/>，操作权限（含角色）与团队行级数据权限在工厂边界强制执行。</description></item>
/// </list>
/// 演示角色：开发（可建可改本团队仓库）、测试（只读本团队仓库）、项目管理（含删除本团队仓库）。
/// 演示数据：u-1 开发归属 T-1/T-2；u-2 测试归属 T-1；u-3 项目管理归属 T-3。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RepositoryController(IObjectFactory factory, IScopeGuard guard, IRepositoryStore store) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> List(CancellationToken cancellationToken)
	{
		// 读侧：把数据权限下推为查询条件，越权行在数据库侧被过滤。
		// OrderBy 必须落在投影之前：EF Core 无法翻译投影后对 DTO 成员的排序，会查询翻译失败。
		var repositories = await guard.Apply(store.Query())
		                              .OrderBy(repository => repository.Id)
		                              .Select(repository => new RepositoryDto(repository.Id, repository.Name, repository.TeamId))
		                              .ToListAsync(cancellationToken);
		return Ok(repositories);
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
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

	private static RepositoryDto ToDto(CodeRepository repository)
	{
		return new RepositoryDto(repository.Id, repository.Name, repository.TeamId);
	}

	private IActionResult Denied(SecurityException exception)
	{
		return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
	}
}