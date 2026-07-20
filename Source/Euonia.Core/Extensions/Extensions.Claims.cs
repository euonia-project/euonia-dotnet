using System.Security.Authentication;
using System.Security.Claims;
using Nerosoft.Euonia.Security;

/// <summary>
/// <see cref="UserPrincipal"/> 和 <see cref="ClaimsPrincipal"/> 的扩展方法。
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// 获取 <see cref="Guid"/> 类型的用户 ID。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <returns>用户的 GUID。</returns>
    /// <exception cref="FormatException">当用户 ID 格式无效时抛出。</exception>
    public static Guid GetUserIdOfGuid(this UserPrincipal user)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
        {
            throw new AuthenticationException();
        }

        if (!(Guid.TryParse(user.UserId, out var userId)))
        {
            throw new FormatException();
        }

        return userId;
    }

    /// <summary>
    /// 获取 <see cref="long"/> 类型的用户 ID。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <returns>用户的 Int64 ID。</returns>
    /// <exception cref="AuthenticationException">当用户未通过身份验证时抛出。</exception>
    /// <exception cref="FormatException">当用户 ID 格式无效时抛出。</exception>
    public static long GetUserIdOfInt64(this UserPrincipal user)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
        {
            throw new AuthenticationException();
        }

        if (!(long.TryParse(user.UserId, out var userId)))
        {
            throw new FormatException();
        }

        return userId;
    }

    /// <summary>
    /// 获取 <see cref="int"/> 类型的用户 ID。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <returns>用户的 Int32 ID。</returns>
    /// <exception cref="AuthenticationException">当用户未通过身份验证时抛出。</exception>
    /// <exception cref="FormatException">当用户 ID 格式无效时抛出。</exception>
    public static int GetUserIdOfInt32(this UserPrincipal user)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
        {
            throw new AuthenticationException();
        }

        if (!(int.TryParse(user.UserId, out var userId)))
        {
            throw new FormatException();
        }

        return userId;
    }

    /// <summary>
    /// 确保用户已通过身份验证。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <exception cref="AuthenticationException">当用户未通过身份验证时抛出。</exception>
    public static void EnsureAuthenticated(this UserPrincipal user)
    {
        if (!user.IsAuthenticated && !string.IsNullOrWhiteSpace(user.Username) && !string.IsNullOrWhiteSpace(user.UserId))
        {
            throw new AuthenticationException();
        }
    }

    /// <summary>
    /// 确保用户属于指定的角色。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <param name="roles">角色集合。</param>
    /// <param name="message">授权失败时的错误消息。</param>
    /// <exception cref="AuthenticationException">当用户未通过身份验证时抛出。</exception>
    /// <exception cref="UnauthorizedAccessException">当用户不属于指定角色时抛出。</exception>
    public static void EnsureInRoles(this UserPrincipal user, IEnumerable<string> roles, string message)
    {
        user.EnsureAuthenticated();

        if (!user.IsInRoles(roles))
        {
            throw new UnauthorizedAccessException(message);
        }
    }

    /// <summary>
    /// 确保用户属于指定的角色，并执行相应的回调。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <param name="roles">角色集合。</param>
    /// <param name="successCallback">成功时的回调。</param>
    /// <param name="failureCallback">失败时的回调。</param>
    /// <exception cref="AuthenticationException">当用户未通过身份验证时抛出。</exception>
    public static void EnsureInRoles(this UserPrincipal user, IEnumerable<string> roles, Action successCallback = null, Action failureCallback = null)
    {
        user.EnsureAuthenticated();

        if (user.IsInRoles(roles))
        {
            successCallback?.Invoke();
        }
        else
        {
            failureCallback?.Invoke();
        }
    }

    /// <summary>
    /// 异步确保用户属于指定的角色，并执行相应的回调。
    /// </summary>
    /// <param name="user">用户主体。</param>
    /// <param name="roles">角色集合。</param>
    /// <param name="successCallback">成功时的异步回调。</param>
    /// <param name="failureCallback">失败时的异步回调。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <exception cref="AuthenticationException">当用户未通过身份验证时抛出。</exception>
    public static Task EnsureInRolesAsync(this UserPrincipal user, IEnumerable<string> roles, Func<Task> successCallback = null, Func<Task> failureCallback = null)
    {
        user.EnsureAuthenticated();

        if (user.IsInRoles(roles))
        {
            return successCallback?.Invoke();
        }
        else
        {
            return failureCallback?.Invoke();
        }
    }

    /// <summary>
    /// 将 scope 声明规范化为单独的声明类型。
    /// </summary>
    /// <param name="principal">声明主体。</param>
    /// <returns>规范化后的声明主体。</returns>
    public static ClaimsPrincipal NormalizeScopeClaims(this ClaimsPrincipal principal)
    {
        var identities = new List<ClaimsIdentity>();

        foreach (var id in principal.Identities)
        {
            var identity = new ClaimsIdentity(id.AuthenticationType, id.NameClaimType, id.RoleClaimType);

            foreach (var claim in id.Claims)
            {
                if (claim.Type == "scope")
                {
                    if (claim.Value.Contains(' '))
                    {
                        var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var scope in scopes)
                        {
                            identity.AddClaim(new Claim("scope", scope, claim.ValueType, claim.Issuer));
                        }
                    }
                    else
                    {
                        identity.AddClaim(claim);
                    }
                }
                else
                {
                    identity.AddClaim(claim);
                }
            }

            identities.Add(identity);
        }

        return new ClaimsPrincipal(identities);
    }
}