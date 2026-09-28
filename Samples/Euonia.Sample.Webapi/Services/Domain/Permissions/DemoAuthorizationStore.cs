using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Persist;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 某个演示用户的授权数据：角色、持有的权限码（各资源并集）与团队范围。
/// 演示授权数据存于授权数据库中；判定时由
/// <see cref="DemoScopeSubjectResolver"/> 从中实时解析。
/// 权限码是全局的（subject 维度），故此处为仓库、团队等资源权限码的并集。
/// </summary>
public sealed record DemoAuthorization(string Name, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Codes, IReadOnlyCollection<string> Teams);

/// <summary>
/// 演示用的授权数据库（SQLite），通过 <see cref="SampleDataContext"/> 读写。
/// 权限码与行级授予不能固化在令牌中，否则取消授权后旧令牌依然有效，故拒绝（撤销）立即生效、
/// 无需重新签发令牌；角色来自声明（<see cref="RoleName"/>）、随令牌传递，改角色须重新登录生效。
/// 本类提供 <see cref="Nerosoft.Euonia.Sample.Controllers.PermissionController"/> 使用的运行时读写入口。
/// </summary>
public sealed class DemoAuthorizationStore(IApplicationDataContext context)
{
	public async Task<DemoAuthorization> GetAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await LoadAsync(userId, cancellationToken);
	}

	/// <summary>返回全部授权数据的快照（用户标识 → 授权）。</summary>
	public async Task<IReadOnlyDictionary<string, DemoAuthorization>> SnapshotAsync(CancellationToken cancellationToken = default)
	{
		var records = await context.Authorizations.AsNoTracking().ToListAsync(cancellationToken);
		var result = new Dictionary<string, DemoAuthorization>(StringComparer.Ordinal);
		foreach (var group in records.GroupBy(record => record.UserId))
		{
			result[group.Key] = Build(group.ToArray());
		}

		return result;
	}

	/// <summary>新增角色；用户不存在时返回 <see langword="null"/>。</summary>
	public async Task<DemoAuthorization> GrantRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
	{
		return await GrantAsync(userId, AuthorizationKinds.Role, roles, cancellationToken);
	}

	/// <summary>移除角色；用户不存在时返回 <see langword="null"/>。</summary>
	public async Task<DemoAuthorization> RevokeRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
	{
		return await RevokeAsync(userId, AuthorizationKinds.Role, roles, cancellationToken);
	}

	/// <summary>授予权限码（立即生效，无需重新登录）；用户不存在时返回 <see langword="null"/>。</summary>
	public async Task<DemoAuthorization> GrantCodesAsync(string userId, IEnumerable<string> codes, CancellationToken cancellationToken = default)
	{
		return await GrantAsync(userId, AuthorizationKinds.Code, codes, cancellationToken);
	}

	/// <summary>撤销权限码（立即生效，无需重新登录）；用户不存在时返回 <see langword="null"/>。</summary>
	public async Task<DemoAuthorization> RevokeCodesAsync(string userId, IEnumerable<string> codes, CancellationToken cancellationToken = default)
	{
		return await RevokeAsync(userId, AuthorizationKinds.Code, codes, cancellationToken);
	}

	/// <summary>把用户加入团队（扩大行级可见范围，立即生效）；用户不存在时返回 <see langword="null"/>。</summary>
	public async Task<DemoAuthorization> AddTeamsAsync(string userId, IEnumerable<string> teamIds, CancellationToken cancellationToken = default)
	{
		return await GrantAsync(userId, AuthorizationKinds.Team, teamIds, cancellationToken);
	}

	/// <summary>把用户移出团队（立即生效）；用户不存在时返回 <see langword="null"/>。</summary>
	public async Task<DemoAuthorization> RemoveTeamsAsync(string userId, IEnumerable<string> teamIds, CancellationToken cancellationToken = default)
	{
		return await RevokeAsync(userId, AuthorizationKinds.Team, teamIds, cancellationToken);
	}

	private async Task<DemoAuthorization> GrantAsync(string userId, string kind, IEnumerable<string> values, CancellationToken cancellationToken)
	{
		var existing = await context.Authorizations
		                            .Where(record => record.UserId == userId)
		                            .Select(record => new { record.Kind, record.Value })
		                            .ToListAsync(cancellationToken);
		if (existing.Count == 0)
		{
			return null;
		}

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

	private async Task<DemoAuthorization> RevokeAsync(string userId, string kind, IEnumerable<string> values, CancellationToken cancellationToken)
	{
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

	private async Task<DemoAuthorization> LoadAsync(string userId, CancellationToken cancellationToken)
	{
		var records = await context.Authorizations.AsNoTracking()
		                           .Where(record => record.UserId == userId)
		                           .ToListAsync(cancellationToken);
		return records.Count == 0 ? null : Build(records.ToArray());
	}

	private static DemoAuthorization Build(IReadOnlyCollection<AuthorizationRecord> records)
	{
		var userId = records.First().UserId;
		return new DemoAuthorization(
			Split(records, AuthorizationKinds.Name).FirstOrDefault() ?? userId,
			Split(records, AuthorizationKinds.Role),
			Split(records, AuthorizationKinds.Code),
			Split(records, AuthorizationKinds.Team));
	}

	private static IReadOnlyCollection<string> Split(IEnumerable<AuthorizationRecord> records, string kind)
	{
		return records.Where(record => record.Kind == kind).Select(record => record.Value).ToArray();
	}
}