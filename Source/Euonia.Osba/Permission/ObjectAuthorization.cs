using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 操作级授权的<b>唯一</b>实现：收集权限要求、调用宿主的 <see cref="IPermissionChecker"/> 判定，
/// 并由 <see cref="BusinessObjectFactory"/> 在调用业务方法前强制。
/// </summary>
/// <remarks>
/// <para>
/// 要求来自宿主注册的来源（<see cref="IPermissionCodeSource"/>，未注册时回落到 Osba 的默认来源），
/// 判定交给宿主注册的 <see cref="IPermissionChecker"/>。本类只有两副面孔：
/// </para>
/// <list type="bullet">
/// <item><description><b>查询</b>（<see cref="IsGranted"/>）：不抛异常，无从判定时返回 <see langword="true"/>。
/// <see cref="BusinessObject.CanPerformOperation"/> 用的就是它。</description></item>
/// <item><description><b>闸门</b>（<see cref="EnsureAuthorized"/> / <see cref="EnsureAuthorizedAsync"/>）：
/// 拒绝抛 <see cref="SecurityException"/>，判定不了抛 <see cref="InvalidOperationException"/>。</description></item>
/// </list>
/// <para>
/// <b>无法判定时必须失败，不能静默放行</b>：目标声明了权限要求却取不到 <see cref="BusinessContext"/>
/// 属配置错误——多半是调用方 <c>new</c> 出对象后忘了接线。闸门路径这种情况下抛
/// <see cref="InvalidOperationException"/>，而不是当作「没有权限要求」放过去。
/// </para>
/// </remarks>
internal static class ObjectAuthorization
{
	/// <summary>
	/// 校验当前用户是否被允许对目标执行业务操作，拒绝时抛出 <see cref="SecurityException"/>。
	/// </summary>
	/// <param name="target">目标业务对象；非 <see cref="BusinessObject"/> 类型时自动放行。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <exception cref="InvalidOperationException">目标声明了权限要求却无法判定（未接入上下文/未注册判定实现）时抛出。</exception>
	/// <exception cref="SecurityException">当前用户未被授权执行该操作时抛出。</exception>
	internal static void EnsureAuthorized(object target, string operation)
	{
		if (!TryPrepare(target, operation, out var businessObject, out _))
		{
			return;
		}

		Enforce(businessObject, operation);
	}

	/// <summary>
	/// 异步版本的 <see cref="EnsureAuthorized"/>：判定前先把授权数据解析出来，避免冷缓存阻塞线程。
	/// </summary>
	/// <param name="target">目标业务对象；非 <see cref="BusinessObject"/> 类型时自动放行。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <exception cref="InvalidOperationException">目标声明了权限要求却无法判定（未接入上下文/未注册判定实现）时抛出。</exception>
	/// <exception cref="SecurityException">当前用户未被授权执行该操作时抛出。</exception>
	/// <remarks>
	/// 供 <c>BusinessObjectFactory</c> 的 <c>*Async</c> 入口使用。判定本身仍是同步契约，
	/// 这里只把 <see cref="AuthorizationWarmup"/> 挪到判定之前——授权数据命中暖路径后不再走
	/// <c>AsyncContext.Run</c>，因此整条异步链路不再退化成 sync-over-async。
	/// </remarks>
	internal static async ValueTask EnsureAuthorizedAsync(object target, string operation, CancellationToken cancellationToken = default)
	{
		if (!TryPrepare(target, operation, out var businessObject, out var checker))
		{
			return;
		}

		await AuthorizationWarmup.WarmAsync(checker, cancellationToken).ConfigureAwait(false);

		Enforce(businessObject, operation);
	}

	/// <summary>
	/// 判断目标当前是否被允许执行指定操作（<b>查询</b>语义：不抛异常）。
	/// </summary>
	/// <param name="target">目标业务对象。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <returns>无权限要求、或要求全部满足时返回 <see langword="true"/>；无从判定（未接入上下文/未注册判定实现）时同样返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 真正的拦截在工厂边界的闸门（<see cref="EnsureAuthorized"/>），本方法供业务方法内部做条件分支使用。
	/// 无从判定返回 <see langword="true"/> 是刻意的查询口径——它不构成放行通道。
	/// </remarks>
	internal static bool IsGranted(BusinessObject target, string operation)
	{
		var requirements = Requirements(target, operation);

		if (requirements.Count == 0)
		{
			return true;
		}

		var checker = target.BusinessContext?.GetService<IPermissionChecker>();

		return checker == null || requirements.All(requirement => IsSatisfied(checker, requirement));
	}

	/// <summary>
	/// 判定前的全部前置条件：非业务对象、无权限要求直接放行；有要求则校验上下文与判定实现可得。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <param name="businessObject">需要继续判定的业务对象。</param>
	/// <param name="checker">判定实现。</param>
	/// <returns>需要判定则返回 <see langword="true"/>。</returns>
	private static bool TryPrepare(object target, string operation, out BusinessObject businessObject, out IPermissionChecker checker)
	{
		checker = null;

		if (target is not BusinessObject candidate)
		{
			businessObject = null;
			return false;
		}

		var requirements = Requirements(candidate, operation);

		if (requirements.Count == 0)
		{
			// 没有任何权限要求：放行
			businessObject = null;
			return false;
		}

		// 有要求却判定不了 —— 属配置错误，必须暴露
		Check.Ensure(
			candidate.BusinessContext != null,
			Resources.IDS_OBJECT_CONTEXT_MISSING,
			candidate.GetType().Name,
			operation);

		checker = candidate.BusinessContext.GetService<IPermissionChecker>();

		Check.Ensure(
			checker != null,
			Resources.IDS_PERMISSION_CHECKER_MISSING,
			candidate.GetType().Name,
			nameof(IPermissionChecker));

		businessObject = candidate;
		return true;
	}

	private static void Enforce(BusinessObject businessObject, string operation)
	{
		// 唯一入口：内置操作与自定义操作都走 CanPerformOperation（派生类可重写它来定制），
		// 其默认实现即本类的查询判定。此前这里对 5 个内置常量之外的操作写的是 _ => true ——
		// requirements 已收集却从不交给 IPermissionChecker，等于自定义操作（README §3.3 的
		// approve / order:archive 等）没有鉴权。
		if (!businessObject.CanPerformOperation(operation))
		{
			throw new SecurityException(string.Format(Resources.IDS_OPERATION_NOT_ALLOWED, operation, businessObject.GetType().Name));
		}
	}

	/// <summary>
	/// 单条要求的满足判定：权限码与角色两部分各自为空表示该项不作要求，角色之间是「或」。
	/// </summary>
	/// <param name="checker">判定实现。</param>
	/// <param name="requirement">权限要求。</param>
	/// <returns>满足则返回 <see langword="true"/>。</returns>
	private static bool IsSatisfied(IPermissionChecker checker, PermissionAttribute requirement)
	{
		var rolesSatisfied = requirement.Roles is not { Length: > 0 } || requirement.Roles.Any(checker.IsInRole);
		var permissionSatisfied = string.IsNullOrEmpty(requirement.Permission) || checker.IsGranted(requirement.Permission);

		return rolesSatisfied && permissionSatisfied;
	}

	/// <summary>
	/// 收集目标在指定操作上的要求；未注册要求来源时回落到 Osba 的默认来源（工厂约定扫描）。
	/// </summary>
	/// <param name="businessObject">目标业务对象。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限要求列表。</returns>
	private static IReadOnlyList<PermissionAttribute> Requirements(BusinessObject businessObject, string operation)
	{
		var provider = businessObject.BusinessContext?.GetService<IPermissionCodeSource>()
		               ?? ObjectPermissionRequirementProvider.Instance;

		return provider.RequirementsFor(businessObject.GetType(), operation);
	}
}
