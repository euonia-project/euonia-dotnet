using System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 权限控制演示端点：
/// <list type="number">
/// <item><description>POST <c>api/auth/login</c>（<see cref="AuthController"/>）签发带身份的令牌。</description></item>
/// <item><description>GET <c>api/projects</c> 用 <see cref="IScopeGuard.Apply{T}"/> 把数据权限下推为查询条件。</description></item>
/// <item><description>POST/GET/PUT 走 <see cref="IObjectFactory"/>，操作权限与行级数据权限在工厂边界强制执行。</description></item>
/// </list>
/// 演示数据：u-1 有 create/view/edit/delete 且可见 d-1/d-2；u-2 有 create/view/edit 且可见 d-1；
/// u-3 只有 view 且可见 d-3。<c>project:edit</c> 声明为「仅所有者」，因此即使对方可见也未必可改。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProjectController(IObjectFactory factory, IScopeGuard guard, IProjectStore store) : ControllerBase
{
	[HttpGet]
	public IActionResult List()
	{
		// 读侧：把数据权限下推为查询条件，越权行在数据库侧被过滤
		var projects = guard.Apply(store.Query())
		                    .Select(ToDto)
		                    .ToArray();
		return Ok(projects);
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
		try
		{
			var project = await factory.FetchAsync<Project>(id);
			return Ok(ToDto(project));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
	}

	[HttpPost]
	public async Task<IActionResult> CreateAsync([FromBody] ProjectCreateInput input, CancellationToken cancellationToken)
	{
		try
		{
			var project = await factory.CreateAsync<Project>(input.Name, input.DeptId);
			await project.SaveAsync(forceUpdate: true, cancellationToken: cancellationToken);
			return Ok(ToDto(project));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateAsync(string id, [FromBody] ProjectUpdateInput input, CancellationToken cancellationToken)
	{
		try
		{
			var project = await factory.FetchAsync<Project>(id);
			project.Name = input.Name;
			project.DeptId = input.DeptId;
			await project.SaveAsync(forceUpdate: true, cancellationToken: cancellationToken);
			return Ok(ToDto(project));
		}
		catch (SecurityException exception)
		{
			return Denied(exception);
		}
	}

	[HttpGet("{id}/explain")]
	public async Task<IActionResult> ExplainAsync(string id, CancellationToken cancellationToken)
	{
		var project = await factory.FetchAsync<Project>(id);
		return Ok(guard.Explain(project));
	}

	private static ProjectDto ToDto(Project project)
	{
		return new ProjectDto(project.Id, project.Name, project.OwnerId, project.DeptId);
	}

	private IActionResult Denied(SecurityException exception)
	{
		return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
	}
}