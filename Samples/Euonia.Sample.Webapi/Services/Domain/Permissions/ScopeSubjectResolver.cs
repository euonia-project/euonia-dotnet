using System.Security.Claims;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 按当前用户（claim 中的 subject）查找授权数据并解析为权限主体。
/// 团队范围在此处展开为扁平集合交给框架；角色由令牌中的声明提供；
/// 行级授予（<see cref="AuthorizationKinds.Grant"/>，编码 <c>"{op}|{id}"</c>）按操作码分组，
/// 再经 <see cref="ScopeSubjectSetBuilder.AddGrant"/> 下推到对应资源的行级维度：
/// <c>repository:*</c> → <see cref="RepositoryScopeModel.RepositoryDimension"/>、
/// <c>project:*</c> → <see cref="ProjectScopeModel.ProjectDimension"/>；其余前缀一律忽略。
/// </summary>
public sealed class ScopeSubjectResolver : IScopeSubjectResolver
{
	private readonly AuthorizationStore _store;

	public ScopeSubjectResolver(AuthorizationStore store)
	{
		_store = store;
	}

	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		return ResolveCoreAsync(user, cancellationToken);
	}

	private async ValueTask<ScopeSubjectSet> ResolveCoreAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
	{
		// sub 在部分 JwtBearer 版本中被映射成 ClaimTypes.NameIdentifier，两个都认
		var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value
		             ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userId))
		{
			return ScopeSubjectSet.Empty;
		}

		var authorization = await _store.GetAsync(userId, cancellationToken);
		if (authorization == null)
		{
			return ScopeSubjectSet.Empty;
		}

		var builder = ScopeSubjectSet.CreateBuilder()
		                              .AddCodes(authorization.Codes)
		                              .AddSelf(userId);

		foreach (var team in authorization.Teams)
		{
			builder.Add(ScopeDimensions.Team, team);
		}

		foreach (var group in authorization.Grants
		                                 .Select(ParseGrant)
		                                 .Where(grant => grant.HasValue)
		                                 .GroupBy(grant => grant!.Value.Operation))
		{
			var dimension = ResolveDimension(group.Key);
			if (dimension == null)
			{
				continue;
			}

			// 框架按（权限码, 维度）分组查找授予，且是「该码下的授予覆盖默认键」而不是并集：
			// 若只把行级授予挂在被授予的那个操作码下，持 project:edit 行的用户查 project:view 时
			// 会因该码下没有授予而落空。语义上「被授予某行的某个码」本就蕴含「可看该行」，
			// 故读码额外并列挂一份；写码之间仍彼此独立（edit 不蕴含 archive）。
			foreach (var code in ScopeCodesFor(group.Key, dimension))
			{
				builder.AddGrant(code, dimension, group.Select(grant => grant.Value.Id));
			}
		}

		return builder.Build();
	}

	/// <summary>
	/// 某个行级授予应当挂到哪些权限码下：被授予的码本身，加上（仅对读码）「可看该行」的并列授予。
	/// </summary>
	private static IEnumerable<string> ScopeCodesFor(string operation, string dimension)
	{
		yield return operation;

		if (operation == ProjectPermissions.View)
		{
			yield break;
		}

		if (string.Equals(dimension, ProjectScopeModel.ProjectDimension, StringComparison.Ordinal))
		{
			yield return ProjectPermissions.View;
		}
	}

	/// <summary>解析 <c>"{op}|{id}"</c> 形态的行级授予；格式非法时返回 <see langword="null"/>。</summary>
	private static (string Operation, string Id)? ParseGrant(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var index = value.IndexOf('|', StringComparison.Ordinal);
		if (index <= 0 || index == value.Length - 1)
		{
			return null;
		}

		return (value[..index], value[(index + 1)..]);
	}

	/// <summary>按操作码前缀把行级授予路由到相应资源的行级维度；未知前缀忽略。</summary>
	private static string ResolveDimension(string operation)
	{
		if (operation.StartsWith("repository:", StringComparison.Ordinal))
		{
			return RepositoryScopeModel.RepositoryDimension;
		}

		if (operation.StartsWith("project:", StringComparison.Ordinal))
		{
			return ProjectScopeModel.ProjectDimension;
		}

		return null;
	}
}