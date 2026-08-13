namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// IdentityServer 认证的常量。
/// </summary>
public static class IdentityServerAuthenticationDefaults
{
    /// <summary>
    /// 认证方案名称。
    /// </summary>
    public const string AuthenticationScheme = "Bearer";

    /// <summary>
    /// JWT typ 标头的值（IdentityServer4 v3+ 默认设置此值）。
    /// </summary>
    public const string JwtAccessTokenTyp = "at+jwt";

    /// <summary>
    /// 内省认证方案名称。
    /// </summary>
    internal const string IntrospectionAuthenticationScheme = "IdentityServerAuthenticationIntrospection";

    /// <summary>
    /// JWT 认证方案名称。
    /// </summary>
    internal const string JwtAuthenticationScheme = "IdentityServerAuthenticationJwt";

    /// <summary>
    /// 令牌项键。
    /// </summary>
    internal const string TokenItemsKey = "idsrv4:tokenvalidation:token";

    /// <summary>
    /// 生效方案键前缀。
    /// </summary>
    internal const string EffectiveSchemeKey = "idsrv4:tokenvalidation:effective:";
}