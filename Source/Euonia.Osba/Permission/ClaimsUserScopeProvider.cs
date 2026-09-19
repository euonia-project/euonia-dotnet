using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 从 <c>scope:{维度}</c> 声明解析用户范围的可选提供者实现。
/// </summary>
/// <remarks>
/// <para>
/// 声明通常固化在访问令牌中，无法实时反映团队、资源及其授权的增删改，
/// 仅在"授权变更可延迟生效"的场景下使用，不作为默认提供者。
/// 真实系统应由应用提供基于授权数据的 <see cref="IUserScopeProvider"/> 实现。
/// </para>
/// <para>
/// 声明约定：类型为 <see cref="UserClaimTypes.ScopePrefix"/> 前缀加上维度名，
/// 值为该维度的不透明值；值 "*" 表示该维度通配，维度 "*" 且值 "*" 表示全局通配。
/// </para>
/// </remarks>
public class ClaimsUserScopeProvider : IUserScopeProvider
{
	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ResolveScopes(UserPrincipal user)
	{
		if (user == null || !user.IsAuthenticated)
		{
			return Array.Empty<ScopeTag>();
		}

		var scopes = new List<ScopeTag>();
		foreach (var claim in user.GetAllClaims())
		{
			if (claim.Type?.StartsWith(UserClaimTypes.ScopePrefix, StringComparison.Ordinal) != true)
			{
				continue;
			}

			var dimension = claim.Type[UserClaimTypes.ScopePrefix.Length..];
			scopes.Add(new ScopeTag(dimension, claim.Value));
		}

		return scopes;
	}
}