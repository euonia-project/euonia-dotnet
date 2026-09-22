using System.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 操作级授权的强制执行点，由 <see cref="BusinessObjectFactory"/> 在调用业务方法前调用。
/// </summary>
/// <remarks>
/// <para>
/// <b>无法判定时必须失败，不能静默放行</b>：目标声明了权限要求却取不到
/// <see cref="BusinessContext"/>（<see cref="BusinessObject.CanUpdateObject"/> 之类会因此解析不到
/// <see cref="IPermissionChecker"/>）属配置错误——多半是调用方 <c>new</c> 出对象后忘了接线。
/// 这种情况下抛 <see cref="InvalidOperationException"/>，而不是当作「没有权限要求」放过去。
/// </para>
/// <para>
/// 注意 <see cref="BusinessObject.CanUpdateObject"/> 等方法本身<b>仍是查询</b>：它们不抛异常，
/// 只是无从判定时返回 <see langword="true"/>。本类才是闸门。
/// </para>
/// </remarks>
internal static class ObjectAuthorization
{
	/// <summary>
	/// 校验当前用户是否被允许对目标执行业务操作，拒绝时抛出 <see cref="SecurityException"/>。
	/// </summary>
	/// <param name="target">目标业务对象；非 <see cref="BusinessObject"/> 类型时自动放行。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <exception cref="InvalidOperationException">目标声明了权限要求却无法判定（未接入上下文/未注册检查器）时抛出。</exception>
	/// <exception cref="SecurityException">当前用户未被授权执行该操作时抛出。</exception>
	internal static void EnsureAuthorized(object target, BusinessOperation operation)
	{
		if (target is not BusinessObject businessObject)
		{
			return;
		}

		var requirements = PermissionRequirements.For(businessObject.GetType(), operation);

		if (requirements.Count == 0)
		{
			// 没有任何权限要求：放行
			return;
		}

		// 有要求却判定不了 —— 属配置错误，必须暴露
		Check.Ensure(
			businessObject.BusinessContext != null,
			"业务对象 '{0}' 声明了权限要求，但未接入 BusinessContext，无法判定 {1}。"
			+ "请通过工厂创建/读取对象，或在调用前设置 BusinessContext。",
			businessObject.GetType().Name,
			operation);

		Check.Ensure(
			businessObject.BusinessContext.GetService<IPermissionChecker>() != null,
			"业务对象 '{0}' 声明了权限要求，但容器中未注册 {1}。",
			businessObject.GetType().Name,
			nameof(IPermissionChecker));

		var allowed = operation switch
		{
			BusinessOperation.Read => businessObject.CanReadObject(),
			BusinessOperation.Create => businessObject.CanCreateObject(),
			BusinessOperation.Update => businessObject.CanUpdateObject(),
			BusinessOperation.Delete => businessObject.CanDeleteObject(),
			BusinessOperation.Execute => businessObject.CanExecuteObject(),
			_ => false
		};

		if (!allowed)
		{
			throw new SecurityException($"Operation not permitted. {operation}: {target.GetType().Name}");
		}
	}
}
