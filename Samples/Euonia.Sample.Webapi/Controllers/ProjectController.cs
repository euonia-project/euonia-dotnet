using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 项目端点：通过 <see cref="IProjectApplicationService"/> 承载读写。
/// 写侧操作权限（project:create / edit / archive / delete）与行级数据权限在工厂边界强制执行，
/// 越权与不存在由全局异常中间件映射为 403 / 404。
/// 项目不对外公开：列表与详情对匿名一律不可见。
/// 状态码约定：查询 200、创建 201、更新/归档/删除 204。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class ProjectController(IProjectApplicationService service) : ControllerBase
{
	/// <summary>分页查询项目（已登录须持有 project:view 码并按行范围过滤；项目不公开）。</summary>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public Task<IActionResult> List([FromQuery] string keyword = null, [FromQuery] string sortBy = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
	{
		return InvokeAsync(() => service.FindAsync(keyword, sortBy, page, pageSize, cancellationToken));
	}

	/// <summary>查看项目详情（行级视图判定；归档项目仍可查看）。</summary>
	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
		return InvokeAsync(() => service.GetAsync(id, cancellationToken));
	}

	/// <summary>创建项目（项目名；描述可省略）。</summary>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> CreateAsync([FromBody] ProjectCreateInput input, CancellationToken cancellationToken)
	{
		var id = await service.CreateAsync(input, cancellationToken);
		return Created($"/api/project/{id}", new { id });
	}

	/// <summary>更新项目（仅名称/描述；归档项目不可改）。</summary>
	[HttpPut("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UpdateAsync(string id, [FromBody] ProjectUpdateInput input, CancellationToken cancellationToken)
	{
		await service.UpdateAsync(id, input, cancellationToken);
		return NoContent();
	}

	/// <summary>归档项目（归档即只读凝固：仍可查看，不可再编辑/归档/删除）。</summary>
	[HttpPost("{id}/archive")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> ArchiveAsync(string id, CancellationToken cancellationToken)
	{
		await service.ArchiveAsync(id, cancellationToken);
		return NoContent();
	}

	/// <summary>删除项目（归档项目不可删）。</summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
	{
		await service.DeleteAsync(id, cancellationToken);
		return NoContent();
	}

	/// <summary>解释项目在当前用户数据范围内的判定结论（调试用）。</summary>
	[HttpGet("{id}/explain")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> ExplainAsync(string id, CancellationToken cancellationToken)
	{
		return InvokeAsync(() => service.ExplainAsync(id, cancellationToken));
	}

	private async Task<IActionResult> InvokeAsync<T>(Func<Task<T>> handler)
	{
		return Ok(await handler());
	}
}