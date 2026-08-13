using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// 用于将作用域（scope）声明从空格分隔转换为独立声明的声明转换器。
/// </summary>
public class ScopeClaimsTransformer : IClaimsTransformation
{
    /// <summary>
    /// 转换主体：调用 NormalizeScopeClaims 将空格分隔的作用域声明拆分为独立声明。
    /// </summary>
    /// <param name="principal">要转换的 ClaimsPrincipal 实例。</param>
    /// <returns>转换后的 ClaimsPrincipal 实例。</returns>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        return Task.FromResult(principal.NormalizeScopeClaims());
    }
}