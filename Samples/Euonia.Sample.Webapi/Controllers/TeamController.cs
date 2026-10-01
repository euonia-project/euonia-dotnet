using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 团队端点：通过 <see cref="ITeamApplicationService"/> 承载读写。
/// 团队行级范围为「本团队成员或负责人」，编辑/删除仅负责人；
/// 列表读侧必须持有 <c>team:view</c> 码，否则返回空页。
/// 状态码约定：查询 200、创建 201、更新/删除 204。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class TeamController(ITeamApplicationService service) : ControllerBase
{
	/// <summary>分页查询团队（须持有 team:view 码并按行范围过滤）。</summary>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public Task<IActionResult> List([FromQuery] string keyword = null, [FromQuery] string sortBy = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
	{
		return InvokeAsync(() => service.FindAsync(keyword, sortBy, page, pageSize, cancellationToken));
	}

	/// <summary>查看团队详情（行级视图判定）。</summary>
	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
	{
		return InvokeAsync(() => service.GetAsync(id, cancellationToken));
	}

	/// <summary>创建团队（负责人取当前用户）。</summary>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> CreateAsync([FromBody] TeamCreateInput input, CancellationToken cancellationToken)
	{
		var id = await service.CreateAsync(input, cancellationToken);
		return Created($"/api/team/{id}", new { id });
	}

	/// <summary>更新团队（仅名称）。</summary>
	[HttpPut("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UpdateAsync(string id, [FromBody] TeamUpdateInput input, CancellationToken cancellationToken)
	{
		await service.UpdateAsync(id, input, cancellationToken);
		return NoContent();
	}

	/// <summary>删除团队。</summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
	{
		await service.DeleteAsync(id, cancellationToken);
		return NoContent();
	}

	/// <summary>
	/// 把账号加入团队（<c>team:edit</c>，行级仅本团队负责人）。
	/// 成员关系的增删即授权变更——团队的可见性由 <c>team_member</c> 实时判定，
	/// 因此这条写入口本身就是授权面。
	/// </summary>
	[HttpPost("{id}/members")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> AddMemberAsync(string id, [FromBody] TeamMemberChangeInput input, CancellationToken cancellationToken)
	{
		await service.AddMemberAsync(id, input.UserId, cancellationToken);
		return NoContent();
	}

	/// <summary>把账号移出团队（<c>team:edit</c>，行级仅本团队负责人）。</summary>
	[HttpDelete("{id}/members/{userId}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> RemoveMemberAsync(string id, string userId, CancellationToken cancellationToken)
	{
		await service.RemoveMemberAsync(id, userId, cancellationToken);
		return NoContent();
	}

	private async Task<IActionResult> InvokeAsync<T>(Func<Task<T>> handler)
	{
		return Ok(await handler());
	}
}