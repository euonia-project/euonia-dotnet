using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 仓库端点：通过 <see cref="IRepositoryApplicationService"/> 承载读写。
/// 写侧操作权限（repository:create / push(update) / delete）与行级数据权限在工厂边界强制执行，
/// 越权与不存在由全局异常中间件映射为 403 / 404。
/// 状态码约定：查询 200、创建 201、更新/删除 204。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class RepositoryController(IRepositoryApplicationService service) : ControllerBase
{
	/// <summary>分页查询仓库（匿名可见公开行；已登录须持有 repository:view 码并按行范围过滤）。</summary>
	[AllowAnonymous]
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public Task<IActionResult> List([FromQuery] string keyword = null, [FromQuery] string sortBy = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
	{
		return InvokeAsync(() => service.FindAsync(keyword, sortBy, page, pageSize, cancellationToken));
	}

	/// <summary>查看仓库详情（行级视图判定）。</summary>
	[AllowAnonymous]
	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
		return InvokeAsync(() => service.GetAsync(id, cancellationToken));
	}

	/// <summary>创建仓库（仓库名 + 归属团队；密级与公开性可省略）。</summary>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> CreateAsync([FromBody] RepositoryCreateInput input, CancellationToken cancellationToken)
	{
		var id = await service.CreateAsync(input, cancellationToken);
		return Created($"/api/repository/{id}", new { id });
	}

	/// <summary>更新仓库（仅名称/密级/公开性；push 边界）。</summary>
	[HttpPut("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UpdateAsync(string id, [FromBody] RepositoryUpdateInput input, CancellationToken cancellationToken)
	{
		await service.UpdateAsync(id, input, cancellationToken);
		return NoContent();
	}

	/// <summary>删除仓库。</summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
	{
		await service.DeleteAsync(id, cancellationToken);
		return NoContent();
	}

	/// <summary>对仓库执行 push（行级 push 判定；update 与其共用同一权限码）。</summary>
	[HttpPost("{id}/push")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> PushAsync(string id, CancellationToken cancellationToken)
	{
		await service.PushAsync(id, cancellationToken);
		return Ok(new { id });
	}

	/// <summary>解释仓库在当前用户数据范围内的判定结论（调试用）。</summary>
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