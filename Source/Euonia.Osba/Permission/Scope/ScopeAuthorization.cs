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

	private static void Ensure(object target, string operation, string stage)
	{
		if (target is not IBusinessObject businessObject)
		{
			return;
		}

		// 未接入业务上下文时，退而用环境上下文（AsyncLocal）查明「这个类型是否受数据权限约束」——
		// 该查询必须能在没有请求作用域时回答（见 IObjectScopeAuthorizer.IsConstrained）。
		var context = businessObject.BusinessContext;
		var authorizer = context?.CurrentServiceProvider.GetService<IObjectScopeAuthorizer>()
		                 ?? BusinessContextAccessor.Current?.GetService<IObjectScopeAuthorizer>();

		if (authorizer == null || !authorizer.IsConstrained(target.GetType()))
		{
			// 未启用数据权限，或该类型未声明权限模型：不受数据权限约束
			return;
		}

		// 已声明模型却拿不到上下文：无法判定，属配置错误（多半是忘了接线），不能静默放行
		Check.Ensure(
			context != null,
			Resources.IDS_SCOPE_CONTEXT_MISSING,
			target.GetType().Name,
			operation);

		// 判定与策略键解析都在实现里（操作是权威，不从对象状态推断——判定可能发生在业务方法返回之后）；
		// 作用域用对象自己的那一个，实现不得依赖环境上下文
		if (!authorizer.Allows(target, operation, context.CurrentServiceProvider))
		{
			throw new SecurityException(
				$"Data scope denied. {operation} ({stage}): {target.GetType().Name}. {authorizer.Explain(target, operation, context.CurrentServiceProvider)}");
		}
	}
}
