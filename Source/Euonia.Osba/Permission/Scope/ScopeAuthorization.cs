using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限在工厂边界的强制执行点，与操作权限的 <see cref="ObjectAuthorization"/> 并列。
/// </summary>
/// <remarks>
/// <para>
/// <b>前置 / 后置的划分依据</b>：目标对象在调用业务方法<b>之前</b>是否已经承载了数据。
/// </para>
/// <list type="table">
/// <item>
///   <term>前置（<see cref="EnsureAuthorizedBefore"/>）</term>
///   <description>目标由调用方提供且已填充（<c>SaveAsync(target)</c>、<c>ExecuteAsync(target)</c>）。
///   判定失败即抛出，业务方法不会执行，无副作用。</description>
/// </item>
/// <item>
///   <term>后置（<see cref="EnsureAuthorizedAfter"/>）</term>
///   <description>目标由工厂方法内部填充（各类 <c>*Async(criteria)</c>、<c>Fetch</c>）。
///   调用前目标还是空对象，范围列尚未赋值，此时判定会误杀一切；
///   因此改在业务方法返回后判定。</description>
/// </item>
/// </list>
/// <para>
/// <b>已知边界</b>：后置检查发生在业务方法返回之后。若业务方法内部已经落库，
/// 它阻止的是「越权对象返回给调用方」，而不是「越权数据写入」——后者需要由持久化层
/// （如 EF 的 SaveChanges 拦截器）或数据库约束保证，Osba 不提供。
/// </para>
/// <para>
/// 未声明权限模型的资源类型不受数据权限约束；没有任何用户上下文时同样不做判定。
/// </para>
/// </remarks>
internal static class ScopeAuthorization
{
	/// <summary>
	/// 在调用业务方法<b>之前</b>校验目标是否在当前用户的数据范围内。
	/// </summary>
	/// <param name="target">目标对象（已由调用方填充）。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <exception cref="SecurityException">目标越出当前用户的数据范围时抛出。</exception>
	internal static void EnsureAuthorizedBefore(object target, string operation)
	{
		Ensure(target, operation, "before");
	}

	/// <summary>
	/// 在业务方法<b>返回之后</b>校验目标是否在当前用户的数据范围内。
	/// </summary>
	/// <param name="target">目标对象（已由业务方法填充）。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <exception cref="SecurityException">目标越出当前用户的数据范围时抛出。</exception>
	internal static void EnsureAuthorizedAfter(object target, string operation)
	{
		Ensure(target, operation, "after");
	}

	/// <summary>
	/// 异步版本的 <see cref="EnsureAuthorizedBefore"/>：判定前先把授权数据解析出来，避免冷缓存阻塞线程。
	/// </summary>
	/// <param name="target">目标对象（已由调用方填充）。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <exception cref="SecurityException">目标越出当前用户的数据范围时抛出。</exception>
	internal static ValueTask EnsureAuthorizedBeforeAsync(object target, string operation, CancellationToken cancellationToken = default)
	{
		return EnsureAsync(target, operation, "before", cancellationToken);
	}

	/// <summary>
	/// 异步版本的 <see cref="EnsureAuthorizedAfter"/>：判定前先把授权数据解析出来，避免冷缓存阻塞线程。
	/// </summary>
	/// <param name="target">目标对象（已由工厂方法填充）。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <exception cref="SecurityException">目标越出当前用户的数据范围时抛出。</exception>
	internal static ValueTask EnsureAuthorizedAfterAsync(object target, string operation, CancellationToken cancellationToken = default)
	{
		return EnsureAsync(target, operation, "after", cancellationToken);
	}

	private static void Ensure(object target, string operation, string stage)
	{
		if (!TryPrepare(target, operation, out var context, out var authorizer))
		{
			return;
		}

		// 同步入口：等待点只在这里（见 AuthorizationWarmup 的说明）
		AuthorizationWarmup.Warm(authorizer, context.CurrentServiceProvider);

		Enforce(target, operation, stage, context, authorizer);
	}

	private static async ValueTask EnsureAsync(object target, string operation, string stage, CancellationToken cancellationToken)
	{
		if (!TryPrepare(target, operation, out var context, out var authorizer))
		{
			return;
		}

		// 判定与策略键解析都要读授权数据（GetPolicy → GetSubjects），冷缓存会阻塞调用线程；
		// 作用域必须用判定时那一个（与 Allows 传参同源），不能退化成环境上下文
		await AuthorizationWarmup.WarmAsync(authorizer, context.CurrentServiceProvider, cancellationToken).ConfigureAwait(false);

		Enforce(target, operation, stage, context, authorizer);
	}

	/// <summary>
	/// 判定前的全部前置条件：不受数据权限约束直接放行；受约束却拿不到上下文则视为配置错误。
	/// </summary>
	private static bool TryPrepare(object target, string operation, out BusinessContext context, out IObjectScopeAuthorizer authorizer)
	{
		context = null;
		authorizer = null;

		if (target is not IBusinessObject businessObject)
		{
			return false;
		}

		// 未接入业务上下文时，退而用环境上下文（AsyncLocal）查明「这个类型是否受数据权限约束」——
		// 该查询必须能在没有请求作用域时回答（见 IObjectScopeAuthorizer.IsConstrained）。
		context = businessObject.BusinessContext;
		authorizer = context?.CurrentServiceProvider.GetService<IObjectScopeAuthorizer>()
		             ?? BusinessContextAccessor.Current?.GetService<IObjectScopeAuthorizer>();

		if (authorizer == null || !authorizer.IsConstrained(target.GetType()))
		{
			// 未启用数据权限，或该类型未声明权限模型：不受数据权限约束
			context = null;
			return false;
		}

		// 已声明模型却拿不到上下文：无法判定，属配置错误（多半是忘了接线），不能静默放行
		Check.Ensure(
			context != null,
			Resources.IDS_SCOPE_CONTEXT_MISSING,
			target.GetType().Name,
			operation);

		return true;
	}

	private static void Enforce(object target, string operation, string stage, BusinessContext context, IObjectScopeAuthorizer authorizer)
	{
		// 判定与策略键解析都在实现里（操作是权威，不从对象状态推断——判定可能发生在业务方法返回之后）；
		// 作用域用对象自己的那一个，实现不得依赖环境上下文
		if (!authorizer.Allows(target, operation, context.CurrentServiceProvider))
		{
			throw new SecurityException(
				string.Format(
					Resources.IDS_SCOPE_DENIED,
					operation,
					stage,
					target.GetType().Name,
					authorizer.Explain(target, operation, context.CurrentServiceProvider)));
		}
	}
}
