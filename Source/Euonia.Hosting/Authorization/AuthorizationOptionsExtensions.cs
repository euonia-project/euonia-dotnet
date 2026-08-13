namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// AuthorizationOptions 的扩展方法。
/// </summary>
public static class AuthorizationOptionsExtensions
{
    /// <summary>
    /// 添加一个用于检查一个或多个作用域（scope）声明是否存在的授权策略。
    /// </summary>
    /// <param name="options">授权选项。</param>
    /// <param name="policyName">策略名称。</param>
    /// <param name="scopes">策略要求的作用域列表。</param>
    /// <returns>配置后的 <see cref="AuthorizationOptions"/> 实例。</returns>
    public static AuthorizationOptions AddScopePolicy(this AuthorizationOptions options, string policyName, params string[] scopes)
    {
        options.AddPolicy(policyName, p =>
        {
            p.RequireAuthenticatedUser();
            p.RequireScope(scopes);
        });

        return options;
    }
}