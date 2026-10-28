using System.Collections.Concurrent;
using System.Diagnostics;
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
	/// <param name="requirementType">
	/// 收集权限要求时使用的类型；为 <see langword="null"/> 时使用目标的运行时类型。
	/// 工厂按静态类型查找并调用方法时应传入该静态类型，以覆盖被 <c>new</c> 遮蔽的基类方法上的要求。
	/// </param>
	/// <exception cref="InvalidOperationException">目标声明了权限要求却无法判定（未接入上下文/未注册判定实现）时抛出。</exception>
	/// <exception cref="SecurityException">当前用户未被授权执行该操作时抛出。</exception>
	internal static void EnsureAuthorized(object target, string operation, Type requirementType = null)
	{
		if (!TryPrepare(target, operation, requirementType, out var businessObject, out var checker, out var requirements))
		{
			return;
		}

		// 同步入口：等待点只在这里（见 AuthorizationWarmup 的说明）
		AuthorizationWarmup.Warm(checker);

		Enforce(businessObject, operation, requirements, checker);
	}

	/// <summary>
	/// 异步版本的 <see cref="EnsureAuthorized"/>：判定前先把授权数据解析出来，避免冷缓存阻塞线程。
	/// </summary>
	/// <param name="target">目标业务对象；非 <see cref="BusinessObject"/> 类型时自动放行。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <param name="requirementType">
	/// 收集权限要求时使用的类型；为 <see langword="null"/> 时使用目标的运行时类型。
	/// 工厂按静态类型查找并调用方法时应传入该静态类型，以覆盖被 <c>new</c> 遮蔽的基类方法上的要求。
	/// </param>
	/// <exception cref="InvalidOperationException">目标声明了权限要求却无法判定（未接入上下文/未注册判定实现）时抛出。</exception>
	/// <exception cref="SecurityException">当前用户未被授权执行该操作时抛出。</exception>
	/// <remarks>
	/// 供 <c>BusinessObjectFactory</c> 的 <c>*Async</c> 入口使用。判定本身仍是同步契约，
	/// 这里只把 <see cref="AuthorizationWarmup"/> 挪到判定之前——授权数据命中暖路径后不再走
	/// <c>AsyncContext.Run</c>，因此整条异步链路不再退化成 sync-over-async。
	/// </remarks>
	internal static async ValueTask EnsureAuthorizedAsync(object target, string operation, CancellationToken cancellationToken = default, Type requirementType = null)
	{
		if (!TryPrepare(target, operation, requirementType, out var businessObject, out var checker, out var requirements))
		{
			return;
		}

		await AuthorizationWarmup.WarmAsync(checker, cancellationToken).ConfigureAwait(false);

		Enforce(businessObject, operation, requirements, checker);
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
	/// <param name="requirementType">
	/// 收集权限要求时使用的类型；为 <see langword="null"/> 时使用目标的运行时类型。
	/// </param>
	/// <param name="businessObject">需要继续判定的业务对象。</param>
	/// <param name="checker">判定实现。</param>
	/// <param name="requirements">为目标收集到的权限要求。</param>
	/// <returns>需要判定则返回 <see langword="true"/>。</returns>
	private static bool TryPrepare(object target, string operation, Type requirementType, out BusinessObject businessObject, out IPermissionChecker checker, out IReadOnlyList<PermissionAttribute> requirements)
	{
		checker = null;
		requirements = [];

		if (target is not BusinessObject candidate)
		{
			businessObject = null;
			return false;
		}

		requirements = Requirements(candidate, operation, requirementType);

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

	/// <summary>
	/// 强制校验目标是否满足操作要求，不满足时抛出 <see cref="SecurityException"/>。
	/// </summary>
	/// <param name="businessObject">目标业务对象。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <param name="requirements">按调用方静态类型收集到的权限要求。</param>
	/// <param name="checker">判定实现。</param>
	/// <exception cref="SecurityException">目标不满足任一权限要求时抛出。</exception>
	private static void Enforce(BusinessObject businessObject, string operation, IReadOnlyList<PermissionAttribute> requirements, IPermissionChecker checker)
	{
		var started = Stopwatch.GetTimestamp();
		var succeeded = false;
		try
		{
			// 先按调用方传入的 requirements 判定一次：这是权威集合，能覆盖「派生类型用 new 遮蔽
			// 基类工厂方法」的运行时类型收集缺口。
			foreach (var requirement in requirements)
			{
				if (!IsSatisfied(checker, requirement))
				{
					throw new SecurityException(string.Format(Resources.IDS_OPERATION_NOT_ALLOWED, operation, businessObject.GetType().Name));
				}
			}

			// 仅当派生类真的重写了 CanPerformOperation 时才再调用一次：默认实现会重新收集
			// requirements 并逐条判定，等于把整条规则再跑一遍（授权数据未命中缓存时就是两倍 I/O）。
			if (IsCanPerformOperationOverridden(businessObject.GetType()) &&
			    !businessObject.CanPerformOperation(operation))
			{
				throw new SecurityException(string.Format(Resources.IDS_OPERATION_NOT_ALLOWED, operation, businessObject.GetType().Name));
			}

			succeeded = true;
		}
		finally
		{
			OsbaDiagnostics.RecordAuthorization(started, "object", operation, succeeded);
		}
	}

	/// <summary>
	/// 判断类型是否重写了 <see cref="BusinessObject.CanPerformOperation(string)"/>。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <returns>重写则返回 <see langword="true"/>。</returns>
	private static bool IsCanPerformOperationOverridden(Type type)
	{
		return _canPerformOverrides.GetOrAdd(type, static candidate =>
		{
			var method = candidate.GetMethod(nameof(BusinessObject.CanPerformOperation), [typeof(string)]);
			return method != null && method.DeclaringType != typeof(BusinessObject);
		});
	}

	/// <summary>
	/// <see cref="BusinessObject.CanPerformOperation(string)"/> 是否被重写的缓存。
	/// </summary>
	private static readonly ConcurrentDictionary<Type, bool> _canPerformOverrides = new();

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
	/// 收集目标在指定操作上的要求；未注册权限码来源时回落到 Osba 的默认来源（工厂约定扫描）。
	/// </summary>
	/// <param name="businessObject">目标业务对象。</param>
	/// <param name="operation">业务操作。</param>
	/// <param name="requirementType">
	/// 收集权限要求时使用的类型；为 <see langword="null"/> 时使用目标的运行时类型。
	/// </param>
	/// <returns>权限要求列表。</returns>
	private static IReadOnlyList<PermissionAttribute> Requirements(BusinessObject businessObject, string operation, Type requirementType = null)
	{
		// 不能只看业务对象自身的上下文：手工构造/反序列化的对象没有 BusinessContext，
		// 但宿主注册的来源仍应生效；与 ScopeAuthorization 一致地回退到环境作用域。
		var provider = businessObject.BusinessContext?.GetService<IPermissionCodeSource>()
		               ?? BusinessContextAccessor.Current?.GetService<IPermissionCodeSource>()
		               ?? ObjectPermissionCodeSource.Instance;

		// 工厂按静态类型查找并调用方法；若目标是派生类型且用 new 遮蔽了基类方法，
		// 按运行时类型收集会漏掉实际被调用的基类方法上的要求。调用方应传入工厂使用的静态类型。
		return provider.RequirementsFor(requirementType ?? businessObject.GetType(), operation);
	}
}
