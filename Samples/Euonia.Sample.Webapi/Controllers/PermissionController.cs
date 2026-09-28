using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 演示授权管理端点：查看/编辑用户的角色、权限码与团队范围。
/// 权限码与团队范围的变更由 <see cref="DemoScopeSubjectResolver"/> 在下一次解析时生效，
/// 故撤销即时生效、无需重新签发令牌；角色声明随令牌传递，改角色后须重新登录。
/// 变更类操作仅限项目管理角色（视作许可管理员）。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PermissionController(DemoAuthorizationStore store) : ControllerBase
{
	/// <summary>列出全部演示用户的授权。</summary>
	[HttpGet]
	public async Task<IActionResult> List(CancellationToken cancellationToken)
	{
		var items = (await store.SnapshotAsync(cancellationToken)).Select(pair => AuthorizationView.From(pair.Key, pair.Value)).ToArray();
		return Ok(items);
	}

	/// <summary>查看指定用户的授权。</summary>
	[HttpGet("{userId}")]
	public async Task<IActionResult> Get(string userId, CancellationToken cancellationToken)
	{
		var authorization = await store.GetAsync(userId, cancellationToken);
		if (authorization == null)
		{
			return NotFound($"Unknown demo user '{userId}'.");
		}

		return Ok(AuthorizationView.From(userId, authorization));
	}

	/// <summary>调整指定用户的角色（增/删，可都留空）。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/roles")]
	public Task<IActionResult> UpdateRoles(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.GrantRolesAsync, store.RevokeRolesAsync, cancellationToken);
	}

	/// <summary>授予/撤销指定用户的权限码。撤销即时生效。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/codes")]
	public Task<IActionResult> UpdateCodes(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.GrantCodesAsync, store.RevokeCodesAsync, cancellationToken);
	}

	/// <summary>把指定用户加入/移出团队（行级范围）。即时生效。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/teams")]
	public Task<IActionResult> UpdateTeams(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.AddTeamsAsync, store.RemoveTeamsAsync, cancellationToken);
	}

	/// <summary>授予/撤销指定用户的仓库行级权限（<see cref="RepositoryGrant"/> 编码值，如 <c>"repository:push|r-100"</c>）。即时生效。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/grants")]
	public Task<IActionResult> UpdateGrants(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.AddGrantsAsync, store.RemoveGrantsAsync, cancellationToken);
	}

	/// <summary>演示用户列表（用户标识 + 显示名）。</summary>
	[HttpGet("users")]
	public async Task<IActionResult> Users(CancellationToken cancellationToken)
	{
		var items = (await store.SnapshotAsync(cancellationToken))
		            .Select(pair => new PermissionUserDto(pair.Key, pair.Value.Name))
		            .OrderBy(item => item.UserId)
		            .ToArray();
		return Ok(items);
	}

	private async Task<IActionResult> Mutate(string userId, PermissionChangeInput input, Func<string, IEnumerable<string>, CancellationToken, Task<DemoAuthorization>> grant, Func<string, IEnumerable<string>, CancellationToken, Task<DemoAuthorization>> revoke, CancellationToken cancellationToken)
	{
		var authorization = await grant(userId, input?.Add ?? [], cancellationToken);
		if (authorization == null)
		{
			return NotFound($"Unknown demo user '{userId}'.");
		}

		authorization = await revoke(userId, input?.Remove ?? [], cancellationToken);
		return Ok(AuthorizationView.From(userId, authorization));
	}
}