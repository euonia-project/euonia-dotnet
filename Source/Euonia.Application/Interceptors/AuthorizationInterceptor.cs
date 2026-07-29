using System.Reflection;
using System.Security.Authentication;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Authorization;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <inheritdoc />
public class AuthorizationInterceptor : IInterceptor
{
    private readonly UserPrincipal _user;

    /// <summary>
    /// 
    /// </summary>
    public AuthorizationInterceptor()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    public AuthorizationInterceptor(UserPrincipal user)
        : this()
    {
        _user = user;
    }

    /// <inheritdoc />
    public void Intercept(IInvocation invocation)
    {
        Intercept(invocation.Method);
        invocation.Proceed();
    }

    private void Intercept(MemberInfo method)
    {
        var attribute = method.GetCustomAttribute<AuthorizeAttribute>();

        if (attribute == null)
        {
            return;
        }

        if (_user is not { IsAuthenticated: true })
        {
            throw new AuthenticationException();
        }

        if (string.IsNullOrEmpty(attribute.Roles))
        {
            return;
        }

        var roles = attribute.Roles.Split(',');
        if (!_user.IsInRoles(roles))
        {
            throw new UnauthorizedAccessException("Unauthorized");
        }
    }
}