using System.Security.Claims;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 表示用户身份，提供常见声明值的类型化访问器和辅助方法。
/// </summary>
/// <remarks>
/// 这是围绕 <see cref="ClaimsPrincipal"/> 的轻量包装器，公开了常见的应用程序特定声明值
///（Subject ID、用户名、Code、Tenant、角色）以及查询声明和角色的便捷方法。
/// 该包装器不会修改底层的 <see cref="ClaimsPrincipal"/>。
/// </remarks>
public class UserPrincipal
{
	/// <summary>
	/// 使用提供的声明主体初始化 <see cref="UserPrincipal"/> 类的新实例。
	/// </summary>
	/// <param name="claims">包含用户声明的 <see cref="ClaimsPrincipal"/>。可以为 <c>null</c>，
	/// 此时大多数访问器将相应地返回 <c>null</c> 或 <c>false</c>。</param>
	public UserPrincipal(ClaimsPrincipal claims)
	{
		Claims = claims;
	}

	/// <summary>
	/// 获取底层的 <see cref="ClaimsPrincipal"/> 实例。
	/// </summary>
	public ClaimsPrincipal Claims { get; }

	/// <summary>
	/// 获取用户标识（Subject）。
	/// </summary>
	/// <remarks>
	/// 此属性按以下顺序尝试查找 Subject 标识：
	/// <list type="bullet">
	/// <item><description><see cref="UserClaimTypes.Subject"/></description></item>
	/// <item><description><see cref="ClaimTypes.NameIdentifier"/></description></item>
	/// </list>
	/// 如果找不到匹配的声明，此属性返回 <c>null</c>。
	/// </remarks>
	public string UserId
	{
		get
		{
			return Claims?.Identity?.AuthenticationType switch
			{
				null or "Anonymous" => null,
				// 对于 JWT/Bearer，优先使用 'sub' 声明
				"Jwt" or "Bearer" => Claims.FindFirst(UserClaimTypes.Subject)?.Value,
				// 对于 Windows 身份验证，优先使用 NameIdentifier 声明
				"Windows" => Claims.FindFirst(ClaimTypes.NameIdentifier)?.Value,
				// 对于 Cookie 身份验证，优先使用 NameIdentifier 声明
				"Cookies" or "Cookie" => Claims.FindFirst(ClaimTypes.NameIdentifier)?.Value,
				_ => null
			};
		}
	}

	/// <summary>
	/// 获取用户显示名称。
	/// </summary>
	/// <value>
	/// <see cref="UserClaimTypes.Name"/> 声明的值，如果声明缺失或底层 <see cref="ClaimsPrincipal"/> 为 <c>null</c> 则返回 <c>null</c>。
	/// </value>
	public string Username
	{
		get
		{
			return Claims.Identity?.AuthenticationType switch
			{
				null or "Anonymous" => null,
				// 对于 JWT/Bearer，优先使用 'name' 声明
				"Jwt" or "Bearer" => Claims?.FindFirst(UserClaimTypes.Name)?.Value,
				// 对于 Windows 身份验证，优先使用 Name 声明
				"Windows" => Claims?.FindFirst(ClaimTypes.Name)?.Value,
				// 对于 Cookie 身份验证，优先使用 Name 声明
				"Cookies" or "Cookie" => Claims?.FindFirst(ClaimTypes.Name)?.Value,
				_ => null
			};
		}
	}

	/// <summary>
	/// 获取用户编码。
	/// </summary>
	/// <value>
	/// <see cref="UserClaimTypes.Code"/> 声明的值，如果声明缺失或底层 <see cref="ClaimsPrincipal"/> 为 <c>null</c> 则返回 <c>null</c>。
	/// </value>
	public string Code => Claims?.FindFirst(UserClaimTypes.Code)?.Value;

	/// <summary>
	/// 获取与用户关联的租户标识。
	/// </summary>
	/// <value>
	/// <see cref="UserClaimTypes.Tenant"/> 声明的值。如果声明或底层 <see cref="ClaimsPrincipal"/> 不存在则可能为 <c>null</c>。
	/// </value>
	public string Tenant => Claims.FindFirst(UserClaimTypes.Tenant)?.Value;

	/// <summary>
	/// 获取用户的角色名称序列。
	/// </summary>
	/// <remarks>
	/// 此属性选择所有类型为 <see cref="UserClaimTypes.Role"/> 的声明的值。
	/// 如果底层 <see cref="ClaimsPrincipal"/> 为 <c>null</c>，返回的序列可能为 <c>null</c>。
	/// 调用方应视情况容忍空序列或 <c>null</c>。
	/// </remarks>
	/// <value>角色名称（声明值）的 <see cref="IEnumerable{T}"/> 或 <c>null</c>。</value>
	public IEnumerable<string> Roles => Claims?.FindAll(UserClaimTypes.Role).Select(t => t.Value);

	/// <summary>
	/// 获取一个值，指示用户是否已通过身份验证。
	/// </summary>
	/// <value>当底层 <see cref="ClaimsPrincipal"/> 的身份已通过验证时为 <c>true</c>；否则为 <c>false</c>。
	/// 如果底层主体或身份为 <c>null</c>，则返回 <c>false</c>。</value>
	public bool IsAuthenticated => Claims?.Identity?.IsAuthenticated ?? false;

	/// <summary>
	/// 查找具有指定声明类型的第一个声明。
	/// </summary>
	/// <param name="claimType">要搜索的声明类型。</param>
	/// <returns>找到的第一个匹配 <see cref="Claim"/>；如果未找到则返回 <c>null</c>。</returns>
	public Claim FindClaim(string claimType)
	{
		return Claims.FindFirst(claimType);
	}

	/// <summary>
	/// 查找与指定声明类型匹配的所有声明。
	/// </summary>
	/// <param name="claimType">要搜索的声明类型。</param>
	/// <returns>包含所有匹配 <see cref="Claim"/> 实例的数组。如果没有匹配的声明，则返回空数组。</returns>
	public Claim[] FindClaims(string claimType)
	{
		return Claims.FindAll(claimType).ToArray();
	}

	/// <summary>
	/// 返回底层 <see cref="ClaimsPrincipal"/> 持有的所有声明。
	/// </summary>
	/// <returns>包含底层主体中所有 <see cref="Claim"/> 实例的数组。如果主体没有声明，则返回空数组。</returns>
	public Claim[] GetAllClaims()
	{
		return Claims.Claims.ToArray();
	}

	/// <summary>
	/// 确定当前用户是否属于指定角色。
	/// </summary>
	/// <param name="role">要检查的角色名称。</param>
	/// <returns>如果用户已通过身份验证且属于指定角色，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsInRole(string role)
	{
		return IsAuthenticated && Claims.IsInRole(role);
	}

	/// <summary>
	/// 确定当前用户是否属于任一指定角色。
	/// </summary>
	/// <param name="roles">要检查的角色名称序列。</param>
	/// <returns>如果用户已通过身份验证且至少属于一个指定角色，则为 <c>true</c>；否则为 <c>false</c>。
	/// 如果 <paramref name="roles"/> 为 <c>null</c> 或空，则返回 <c>false</c>。</returns>
	public bool IsInRoles(IEnumerable<string> roles)
	{
		return IsAuthenticated && roles.Any(IsInRole);
	}

	/// <summary>
	/// 确定当前用户是否属于由分隔字符串指定的任一角色。
	/// </summary>
	/// <param name="role">包含角色名称的分隔字符串（例如："Admin,User"）。</param>
	/// <param name="separator">用于在 <paramref name="role"/> 中分隔角色的字符串。默认为 <c>","</c>。</param>
	/// <returns>如果用户已通过身份验证且至少属于一个解析出的角色，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool IsInRoles(string role, string separator = ",")
	{
#if NET5_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(role);
#else
		ArgumentAssert.ThrowIfNull(role, nameof(role));
#endif

		var roles = role.Split(separator, StringSplitOptions.RemoveEmptyEntries);
		return IsInRoles(roles);
	}
}