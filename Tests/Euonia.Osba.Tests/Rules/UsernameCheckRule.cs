using System.Diagnostics;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests.Rules;

/// <summary>
/// 用户名检查规则：在新建状态下禁止使用用户名 <c>admin</c>。
/// </summary>
[ExecuteOnState(ObjectEditState.New)]
public class UsernameCheckRule : ObjectRuleBase
{
	/// <summary>
	/// 执行规则：目标为 <see cref="UserGeneralBusiness"/> 且名称为 <c>admin</c> 时报告 <c>UsernameNotAllowed</c>。
	/// </summary>
	/// <param name="context">规则上下文，提供目标对象与结果收集器。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步执行的任务。</returns>
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// if (context.Target is not UserGeneralBusiness obj)
		// {
		// 	return;
		// }
		//
		// if (string.Equals(obj.Name, "admin", StringComparison.OrdinalIgnoreCase))
		// {
		// 	// Username "admin" is not allowed.
		// 	context.AddErrorResult("UsernameNotAllowed");
		// }

		return Task.Run(() =>
		          {
			          if (context.Target is not UserGeneralBusiness obj)
			          {
				          return;
			          }

			          if (string.Equals(obj.Name, "admin", StringComparison.OrdinalIgnoreCase))
			          {
				          // 不允许使用用户名 "admin"。
				          context.AddErrorResult("UsernameNotAllowed");
			          }
		          }, cancellationToken);
	}
}