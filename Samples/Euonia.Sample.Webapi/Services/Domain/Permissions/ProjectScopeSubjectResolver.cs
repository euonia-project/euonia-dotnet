using System.Security.Claims;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 按当前用户（claim 中的 subject）查找授权数据并解析为权限主体。
/// 层级（如本部门及下级）应在此处展开为扁平集合交给框架。
/// </summary>
public sealed class ProjectScopeSubjectResolver : IScopeSubjectResolver
{
	private readonly ProjectAuthorizationStore _store;

	public ProjectScopeSubjectResolver(ProjectAuthorizationStore store)
	{
		_store = store;
	}

	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		// sub 在部分 JwtBearer 版本中被映射成 ClaimTypes.NameIdentifier，两个都认
		var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value
		             ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userId) || !_store.TryGet(userId, out var authorization))
		{
			return ValueTask.FromResult(ScopeSubjectSet.Empty);
		}

		var builder = ScopeSubjectSet.CreateBuilder()
		                              .AddCodes(authorization.Codes)
		                              .AddSelf(userId);

		foreach (var dept in authorization.Depts)
		{
			builder.Add(ScopeDimensions.Dept, dept);
		}

		return ValueTask.FromResult(builder.Build());
	}
}