using System.Security.Claims;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 按当前用户（claim 中的 subject）查找授权数据并解析为权限主体。
/// 团队范围在此处展开为扁平集合交给框架；角色由令牌中的声明提供。
/// </summary>
public sealed class DemoScopeSubjectResolver : IScopeSubjectResolver
{
	private readonly DemoAuthorizationStore _store;

	public DemoScopeSubjectResolver(DemoAuthorizationStore store)
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

		return builder.Build();
	}
}