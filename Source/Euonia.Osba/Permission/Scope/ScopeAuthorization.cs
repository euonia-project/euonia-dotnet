using System.Security;
using Microsoft.Extensions.DependencyInjection;

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
	internal static void EnsureAuthorizedBefore(object target, BusinessOperation operation)
	{
		Ensure(target, operation, "before");
	}

	/// <summary>
	/// 在业务方法<b>返回之后</b>校验目标是否在当前用户的数据范围内。
	/// </summary>
	/// <param name="target">目标对象（已由业务方法填充）。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <exception cref="SecurityException">目标越出当前用户的数据范围时抛出。</exception>
	internal static void EnsureAuthorizedAfter(object target, BusinessOperation operation)
	{
		Ensure(target, operation, "after");
	}

	private static void Ensure(object target, BusinessOperation operation, string stage)
	{
		if (target is not IBusinessObject businessObject)
		{
			return;
		}

		// 未接入业务上下文时无从判定（与操作权限保持一致）
		var context = businessObject.BusinessContext;
		if (context == null)
		{
			return;
		}

		var registry = context.GetService<ScopeModelRegistry>();
		if (registry == null || !registry.HasDeclarations)
		{
			return;
		}

		// 未声明权限模型的资源类型不受数据权限约束
		var rowType = target.GetType();
		if (!registry.IsDeclared(rowType))
		{
			return;
		}

		var guard = context.GetService<IScopeGuard>();

		// 已声明模型却拿不到判定入口属配置错误：必须暴露，不能静默放行
		Check.Ensure(
			guard != null,
			"资源类型 '{0}' 已声明数据权限模型，但无法解析 {1}。请确认已调用 AddBusinessObject。",
			rowType.FullName,
			nameof(IScopeGuard));

		// 按操作解析策略键（声明了权限码且模型为该码声明了策略时用该码，否则用操作默认键）。
		// 与规则共用 ScopeKeyResolver，两处不可能对「当前是哪个键」得出不同答案。
		registry.TryGetInherited(rowType, out var registration);

		var scopeKey = ScopeKeyResolver.Resolve(registration, registration.Descriptor.ResourceType, operation);

		if (!guard.AllowsObject(target, scopeKey))
		{
			throw new SecurityException(
				$"Data scope denied. {operation} ({stage}): {rowType.Name}. {guard.ExplainObject(target, scopeKey)}");
		}
	}
}
