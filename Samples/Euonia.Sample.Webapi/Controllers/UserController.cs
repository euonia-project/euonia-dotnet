using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Dtos;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 用户端点。
/// </summary>
/// <remarks>
/// 分权策略：查看他人资料 / 搜索 / 建号属于管理员动作，仅 <see cref="RoleName.ProjectManager"/> 可用；
/// 「我的资料」与「改密」对任意已登录用户开放，且一律以 <c>sub</c> 声明里的用户标识为准，
/// 不接受从请求体传 id——否则普通用户改个 body 就能改别人的资料。
/// </remarks>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserController(IUserApplicationService service, UserPrincipal identity) : ControllerBase
{
	/// <summary>查看当前登录用户自己的资料。</summary>
	[HttpGet("me")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetSelfAsync()
	{
		var result = await service.GetAsync(identity.UserId, HttpContext.RequestAborted);
		return Ok(result);
	}

	/// <summary>更新当前登录用户自己的资料（昵称 / 邮箱 / 手机号）。</summary>
	[HttpPut("me")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> UpdateSelfAsync([FromBody] UserUpdateDto data)
	{
		await service.UpdateAsync(identity.UserId, data, HttpContext.RequestAborted);
		return NoContent();
	}

	/// <summary>修改当前登录用户的密码（须校验旧密码）。</summary>
	[HttpPost("me/password")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ChangePasswordAsync([FromBody] UserChangePasswordDto data)
	{
		await service.ChangePasswordAsync(data, HttpContext.RequestAborted);
		return NoContent();
	}

	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpGet("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetAsync(string id)
	{
		var result = await service.GetAsync(id, HttpContext.RequestAborted);
		return Ok(result);
	}

	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpGet("search")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> FindAsync([FromQuery] string keyword, [FromQuery] int skip = 0, [FromQuery] int take = 20)
	{
		var result = await service.FindAsync(keyword ?? string.Empty, skip, take, HttpContext.RequestAborted);
		return Ok(result);
	}

	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<IActionResult> CreateAsync([FromBody] UserCreateDto data)
	{
		var id = await service.CreateAsync(data, HttpContext.RequestAborted);
		return Created($"/api/user/{id}", new { id });
	}
}