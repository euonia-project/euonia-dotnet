using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 授权管理端点：查看/编辑账号的角色、权限码与团队范围。
/// 角色由账号角色表（user_role）承载，随令牌传递，改角色后重新登录生效；
/// 权限码与团队范围由 <see cref="ScopeSubjectResolver"/> 在下一次解析时生效，
/// 故撤销即时生效、无需重新签发令牌。
/// 变更类操作仅限项目管理角色（视作许可管理员）。
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PermissionController(AuthorizationStore store, IUserRepository users) : ControllerBase
{
	/// <summary>列出全部账号的授权。</summary>
	[HttpGet]
	public async Task<IActionResult> List(CancellationToken cancellationToken)
	{
		var snapshot = await store.SnapshotAsync(cancellationToken);
		var accounts = await users.FindAsync(_ => true, ["Roles"], 0, 1000, cancellationToken);
		var items = accounts.Select(user =>
		{
			snapshot.TryGetValue(user.Id, out var security);
			var roles = user.Roles?.Select(role => role.Name).ToArray() ?? [];
			return AuthorizationView.From(user.Id, security?.Name ?? user.Nickname ?? user.Username, roles, security?.Codes ?? [], security?.Teams ?? [], security?.Grants ?? []);
		}).OrderBy(item => item.UserId).ToArray();
		return Ok(items);
	}

	/// <summary>查看指定账号的授权。</summary>
	[HttpGet("{userId}")]
	public async Task<IActionResult> Get(string userId, CancellationToken cancellationToken)
	{
		var view = await ToViewAsync(userId, cancellationToken);
		if (view == null)
		{
			return NotFound($"Unknown user '{userId}'.");
		}

		return Ok(view);
	}

	/// <summary>调整指定账号的角色（增/删，可都留空）。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/roles")]
	public async Task<IActionResult> UpdateRoles(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		var user = await users.FindAsync(item => item.Id == userId && !item.IsDeleted, [], 0, 1, cancellationToken);
		if (user.Count == 0)
		{
			return NotFound($"Unknown user '{userId}'.");
		}

		await users.AddRolesAsync(userId, input?.Add ?? [], cancellationToken);
		await users.RemoveRolesAsync(userId, input?.Remove ?? [], cancellationToken);
		return Ok(await ToViewAsync(userId, cancellationToken));
	}

	/// <summary>授予/撤销指定账号的权限码。撤销即时生效。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/codes")]
	public Task<IActionResult> UpdateCodes(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.GrantCodesAsync, store.RevokeCodesAsync, cancellationToken);
	}

	/// <summary>把指定账号加入/移出团队（行级范围）。即时生效。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/teams")]
	public Task<IActionResult> UpdateTeams(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.AddTeamsAsync, store.RemoveTeamsAsync, cancellationToken);
	}

	/// <summary>授予/撤销指定账号的仓库行级权限（<see cref="RepositoryGrant"/> 编码值，如 <c>"repository:push|&lt;repository-id&gt;"</c>）。即时生效。</summary>
	[Authorize(Roles = RoleName.ProjectManager)]
	[HttpPut("{userId}/grants")]
	public Task<IActionResult> UpdateGrants(string userId, [FromBody] PermissionChangeInput input, CancellationToken cancellationToken)
	{
		return Mutate(userId, input, store.AddGrantsAsync, store.RemoveGrantsAsync, cancellationToken);
	}

	/// <summary>账号列表（账号标识 + 显示名）。</summary>
	[HttpGet("users")]
	public async Task<IActionResult> Users(CancellationToken cancellationToken)
	{
		var accounts = await users.FindAsync(_ => true, [], 0, 1000, cancellationToken);
		var snapshot = await store.SnapshotAsync(cancellationToken);
		var items = accounts
		            .Select(user => new PermissionUserDto(user.Id, snapshot.TryGetValue(user.Id, out var security) ? security.Name : user.Nickname ?? user.Username))
		            .OrderBy(item => item.UserId)
		            .ToArray();
		return Ok(items);
	}

	private async Task<IActionResult> Mutate(string userId, PermissionChangeInput input, Func<string, IEnumerable<string>, CancellationToken, Task<SecurityProfile>> grant, Func<string, IEnumerable<string>, CancellationToken, Task<SecurityProfile>> revoke, CancellationToken cancellationToken)
	{
		var authorization = await grant(userId, input?.Add ?? [], cancellationToken);
		if (authorization == null)
		{
			return NotFound($"Unknown user '{userId}'.");
		}

		await revoke(userId, input?.Remove ?? [], cancellationToken);
		return Ok(await ToViewAsync(userId, cancellationToken));
	}

	private async Task<AuthorizationView> ToViewAsync(string userId, CancellationToken cancellationToken)
	{
		// “未知账号”以账号表为准：账号存在但从未被授权时返回空视图，
		// 而不是把「没有授权行」误判成「账号不存在」。
		var account = await users.FindAsync(item => item.Id == userId && !item.IsDeleted, [], 0, 1, cancellationToken);
		if (account.Count == 0)
		{
			return null;
		}

		var security = await store.GetAsync(userId, cancellationToken);
		var roles = await users.GetRolesAsync(userId, cancellationToken);
		return AuthorizationView.From(userId, security?.Name ?? account[0].Nickname ?? account[0].Username, roles, security?.Codes ?? [], security?.Teams ?? [], security?.Grants ?? []);
	}
}