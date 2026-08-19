using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Authentication;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 方法拦截器，在方法调用前基于 <see cref="AuthorizeAttribute"/> 执行用户身份与角色授权检查。
/// </summary>
/// <remarks>
/// 基于 Castle DynamicProxy 实现，按以下顺序执行授权：
/// <para>1. 通过 <see cref="IServiceScopeFactory"/> 创建作用域并解析 scoped 的 <see cref="UserPrincipal"/>；</para>
/// <para>2. 用户未通过身份验证时抛出 <see cref="AuthenticationException"/>；</para>
/// <para>3. 特性声明了角色（以逗号分隔）且用户不属于任一角色时抛出 <see cref="UnauthorizedAccessException"/>。</para>
/// 方法上未标注 <see cref="AuthorizeAttribute"/> 时不执行任何检查，直接放行。
/// </remarks>
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

	/// <summary>
	/// 在方法调用前依据方法上的 <see cref="AuthorizeAttribute"/> 执行授权检查，随后继续执行被拦截的方法。
	/// </summary>
	/// <param name="invocation">被拦截的方法调用，提供目标方法、实参以及继续执行的入口。</param>
	/// <exception cref="AuthenticationException">用户未通过身份验证时抛出。</exception>
	/// <exception cref="UnauthorizedAccessException">用户不属于特性声明的任一角色时抛出。</exception>
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

	/// <summary>
	/// 执行授权检查：验证用户是否已通过身份验证，并在特性声明角色时验证用户是否属于其中任一角色。
	/// </summary>
	/// <param name="attribute">方法上声明的授权特性。</param>
	/// <exception cref="AuthenticationException">无法解析 <see cref="UserPrincipal"/> 或用户未通过身份验证时抛出。</exception>
	/// <exception cref="UnauthorizedAccessException">用户不属于 <see cref="AuthorizeAttribute.Roles"/> 中的任一角色时抛出。</exception>
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
