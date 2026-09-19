using System.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 操作级授权的强制执行点，由 <see cref="BusinessObjectFactory"/> 在调用业务方法前调用。
/// </summary>
internal static class ObjectAuthorization
{
	/// <summary>
	/// 校验当前用户是否被允许对目标执行业务操作，拒绝时抛出 <see cref="SecurityException"/>。
	/// </summary>
	/// <param name="target">目标业务对象；非 <see cref="BusinessObject"/> 类型时自动放行。</param>
	/// <param name="operation">要执行的操作。</param>
	/// <exception cref="SecurityException">当前用户未被授权执行该操作时抛出。</exception>
	internal static void EnsureAuthorized(object target, BusinessOperation operation)
	{
		if (target is not BusinessObject businessObject)
		{
			return;
		}

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