using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Authentication;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <inheritdoc />
public class AuthorizationInterceptor : IInterceptor
{
	// 缓存每个方法上的授权特性，避免每次拦截调用都执行反射特性查找。
	private static readonly ConcurrentDictionary<(MethodInfo Target, MethodInfo Interface), AuthorizeAttribute> _attributeCache = new();

	private readonly IServiceScopeFactory _scopeFactory;

	/// <summary>
	/// 初始化 <see cref="AuthorizationInterceptor"/> 类的新实例。
	/// </summary>
	/// <param name="scopeFactory">用于在每次拦截时创建作用域以解析 scoped 的 <see cref="UserPrincipal"/>。</param>
	/// <remarks>
	/// <see cref="UserPrincipal"/> 注册为 scoped 服务，不能在构造函数中直接解析（依赖捕获），
	/// 因此仅在方法声明了 <see cref="AuthorizeAttribute"/> 时才创建作用域解析。
	/// </remarks>
	public AuthorizationInterceptor(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
	}

	/// <inheritdoc />
	public void Intercept(IInvocation invocation)
	{
		var method = invocation.MethodInvocationTarget ?? invocation.Method;
		// 代理基于接口创建时，invocation.Method 是接口方法；特性可能标注在接口方法或实现类方法上，
		// 两种位置都查找，避免实现类上的特性被静默忽略。
		var attribute = _attributeCache.GetOrAdd((method, invocation.Method),
			key => key.Target.GetCustomAttribute<AuthorizeAttribute>()
			       ?? key.Interface.GetCustomAttribute<AuthorizeAttribute>());

		if (attribute != null)
		{
			Authorize(attribute);
		}

		invocation.Proceed();
	}

	private void Authorize(AuthorizeAttribute attribute)
	{
		using var scope = _scopeFactory.CreateScope();
		var user = scope.ServiceProvider.GetService<UserPrincipal>();

		if (user is not { IsAuthenticated: true })
		{
			throw new AuthenticationException();
		}

		if (string.IsNullOrEmpty(attribute.Roles))
		{
			return;
		}

		var roles = attribute.Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (!user.IsInRoles(roles))
		{
			throw new UnauthorizedAccessException("Unauthorized");
		}
	}
}
