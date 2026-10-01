using Duende.IdentityModel;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 用于创建作用域（scope）相关授权策略的扩展方法。
/// </summary>
public static class AuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// 添加一个用于检查必需作用域的授权策略。令牌必须至少包含列出的作用域之一。
    /// </summary>
    /// <param name="builder">授权策略构建器。</param>
    /// <param name="scope">所需作用域列表。令牌必须至少包含列出的作用域之一。</param>
    /// <returns>配置后的 <see cref="AuthorizationPolicyBuilder"/> 实例。</returns>
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, params string[] scope)
    {
        return builder.RequireClaim(JwtClaimTypes.Scope, scope);
    }
}

/// <summary>
/// 用于创建作用域相关策略的辅助类。
/// </summary>
public static class ScopePolicy
{
    /// <summary>
    /// 创建一个用于检查必需作用域的授权策略。令牌必须至少包含列出的作用域之一。
    /// </summary>
    /// <param name="scopes">所需作用域列表。令牌必须至少包含列出的作用域之一。</param>
    /// <returns>构建完成的 <see cref="AuthorizationPolicy"/> 实例。</returns>
    public static AuthorizationPolicy Create(params string[] scopes)
    {
        return new AuthorizationPolicyBuilder()
               .RequireScope(scopes)
               .Build();
    }
}