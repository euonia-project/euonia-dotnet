namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 指定应用程序要使用的角色授权策略。
/// </summary>
public class AuthorizeRolesAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 初始化 <see cref="AuthorizeRolesAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="roles">允许访问的角色列表，多个角色用逗号连接。</param>
    public AuthorizeRolesAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}