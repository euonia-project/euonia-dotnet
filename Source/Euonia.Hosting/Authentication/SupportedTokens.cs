namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// 支持的令牌类型。
/// </summary>
public enum SupportedTokens
{
    /// <summary>
    /// 同时支持 JWT 和引用令牌。
    /// </summary>
    Both,

    /// <summary>
    /// 仅支持 JWT。
    /// </summary>
    Jwt,

    /// <summary>
    /// 仅支持引用令牌。
    /// </summary>
    Reference
}