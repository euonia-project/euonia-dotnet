using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Persist;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 某账号的权限数据：权限码（各资源并集）、团队范围与行级授予。
/// 授权数据存于授权表中；判定时由
/// <see cref="ScopeSubjectResolver"/> 从中实时解析。
/// 权限码是全局的（subject 维度），故此处为仓库、团队等资源权限码的并集。
/// 行级授予（<see cref="AuthorizationKinds.Grant"/>）是 <see cref="RepositoryGrant"/>
/// 编码的操作码与仓库 id 拼接值（如 <c>"repository:push|&lt;repository-id&gt;"</c>）。
/// 角色不属于授权维度（见账号角色表 user_role），由令牌传递。
/// </summary>
public sealed record SecurityProfile(string Name, IReadOnlyCollection<string> Codes, IReadOnlyCollection<string> Teams, IReadOnlyCollection<string> Grants);

/// <summary>
/// 授权数据的读写入口。权限码、团队范围与行级授予不能固化在令牌中，否则取消授权后旧令牌依然有效，
/// 故此类提供即时生效的授予/撤销——请求时读取、撤销立即生效、无需重新签发令牌；
/// 角色由账号角色表承载、随令牌传递，改角色后重新登录生效。
/// 授权变更/查看由 <see cref="Nerosoft.Euonia.Sample.Controllers.PermissionController"/> 承载。
/// </summary>
public sealed class AuthorizationStore(IApplicationDataContext context)
{
	public async Task<SecurityProfile> GetAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await LoadAsync(userId, cancellationToken);
	}

	/// <summary>返回全部授权数据的快照（用户标识 → 授权）。</summary>
	public async Task<IReadOnlyDictionary<string, SecurityProfile>> SnapshotAsync(CancellationToken cancellationToken = default)
	{
		var records = await context.Authorizations.AsNoTracking().ToListAsync(cancellationToken);
		var result = new Dictionary<string, SecurityProfile>(StringComparer.Ordinal);
		foreach (var group in records.GroupBy(record => record.UserId))
		{
			result[group.Key] = Build(group.ToArray());
		}

		return result;
	}

	/// <summary>授予权限码（立即生效，无需重新登录）；账号不存在时返回 <see langword="null"/>。</summary>
	public async Task<SecurityProfile> GrantCodesAsync(string userId, IEnumerable<string> codes, CancellationToken cancellationToken = default)
	{
		return await GrantAsync(userId, AuthorizationKinds.Code, codes, cancellationToken);
	}

	/// <summary>撤销权限码（立即生效，无需重新登录）；账号不存在时返回 <see langword="null"/>。</summary>
	public async Task<SecurityProfile> RevokeCodesAsync(string userId, IEnumerable<string> codes, CancellationToken cancellationToken = default)
	{
		return await RevokeAsync(userId, AuthorizationKinds.Code, codes, cancellationToken);
	}

	/// <summary>把用户加入团队（扩大行级可见范围，立即生效）；账号不存在时返回 <see langword="null"/>。</summary>
	public async Task<SecurityProfile> AddTeamsAsync(string userId, IEnumerable<string> teamIds, CancellationToken cancellationToken = default)
	{
		return await GrantAsync(userId, AuthorizationKinds.Team, teamIds, cancellationToken);
	}

	/// <summary>把用户移出团队（立即生效）；账号不存在时返回 <see langword="null"/>。</summary>
	public async Task<SecurityProfile> RemoveTeamsAsync(string userId, IEnumerable<string> teamIds, CancellationToken cancellationToken = default)
	{
		return await RevokeAsync(userId, AuthorizationKinds.Team, teamIds, cancellationToken);
	}

	/// <summary>授予行级权限（<see cref="RepositoryGrant"/> 编码值，立即生效）；账号不存在时返回 <see langword="null"/>。</summary>
	public async Task<SecurityProfile> AddGrantsAsync(string userId, IEnumerable<string> grants, CancellationToken cancellationToken = default)
	{
		return await GrantAsync(userId, AuthorizationKinds.Grant, grants, cancellationToken);
	}

	/// <summary>撤销行级权限（立即生效）；账号不存在时返回 <see langword="null"/>。</summary>
	public async Task<SecurityProfile> RemoveGrantsAsync(string userId, IEnumerable<string> grants, CancellationToken cancellationToken = default)
	{
		return await RevokeAsync(userId, AuthorizationKinds.Grant, grants, cancellationToken);
	}

	private async Task<SecurityProfile> GrantAsync(string userId, string kind, IEnumerable<string> values, CancellationToken cancellationToken)
	{
		if (!await UserExistsAsync(userId, cancellationToken))
		{
			// “账号不存在”以账号表为准，而不是以是否已有授权行为准：
			// 首次授权也允许（新建 Profile），否则就绪的真实账号会被误判为 Unknown user。
			return null;
		}

		var existing = await context.Authorizations
		                            .Where(record => record.UserId == userId)
		                            .Select(record => new { record.Kind, record.Value })
		                            .ToListAsync(cancellationToken);

		foreach (var value in values)
		{
			if (existing.All(record => record.Kind != kind || record.Value != value))
			{
				context.Authorizations.Add(new AuthorizationRecord { UserId = userId, Kind = kind, Value = value });
			}
		}

		await context.SaveChangesAsync(cancellationToken);
		return await LoadAsync(userId, cancellationToken);
	}

	private async Task<SecurityProfile> RevokeAsync(string userId, string kind, IEnumerable<string> values, CancellationToken cancellationToken)
	{
		if (!await UserExistsAsync(userId, cancellationToken))
		{
			return null;
		}

		var matches = await context.Authorizations
		                           .Where(record => record.UserId == userId && record.Kind == kind && values.Contains(record.Value))
		                           .ToListAsync(cancellationToken);
		if (matches.Count > 0)
		{
			context.Authorizations.RemoveRange(matches);
			await context.SaveChangesAsync(cancellationToken);
		}

		return await LoadAsync(userId, cancellationToken);
	}

	private async Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken)
	{
		return await context.Users.AsNoTracking().AnyAsync(record => record.Id == userId, cancellationToken);
	}

	private async Task<SecurityProfile> LoadAsync(string userId, CancellationToken cancellationToken)
	{
		var records = await context.Authorizations.AsNoTracking()
		                           .Where(record => record.UserId == userId)
		                           .ToListAsync(cancellationToken);
		return records.Count == 0 ? null : Build(records.ToArray());
	}

	private static SecurityProfile Build(IReadOnlyCollection<AuthorizationRecord> records)
	{
		var userId = records.First().UserId;
		return new SecurityProfile(
			Split(records, AuthorizationKinds.Name).FirstOrDefault() ?? userId,
			Split(records, AuthorizationKinds.Code),
			Split(records, AuthorizationKinds.Team),
			Split(records, AuthorizationKinds.Grant));
	}

	private static IReadOnlyCollection<string> Split(IEnumerable<AuthorizationRecord> records, string kind)
	{
		return records.Where(record => record.Kind == kind).Select(record => record.Value).ToArray();
	}
}