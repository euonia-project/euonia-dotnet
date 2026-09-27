using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 规则在「保存」与「命令执行」两条路径上的统一强制点，属于<b>验证线</b>——只做数据校验，
/// 与工厂边界的 <see cref="ObjectAuthorization"/>（操作权限）、<see cref="ScopeAuthorization"/>（数据权限）<b>互不相干</b>。
/// </summary>
/// <remarks>
/// <para>
/// 两条路径共用 <see cref="BusinessObject.EnsureRulesAsync"/>，因此失败形态一致：<see cref="ValidationException"/> 且 <c>Errors</c> 携带逐条违规
/// （属性名 + 消息）。可编辑对象在 <see cref="EditableObject{T}.SaveAsync(bool, CancellationToken)"/> 内调用，命令对象在命令体之前调用——命令体不会执行到一半才发现对象不合法。
/// </para>
/// <para>
/// <b>权限线完全不在本类内</b>：规则集合里只有数据校验规则。越权（操作权限 / 数据范围）
/// 一律由工厂边界的 <see cref="ObjectAuthorization"/> / <see cref="ScopeAuthorization"/> 拦截并抛
/// <see cref="System.Security.SecurityException"/>，不受 <see cref="Rules.SuppressRuleChecking"/> 与
/// <c>BypassRuleChecks()</c> 之类规则绕过机制的影响——本类只是验证线的强制点，不是安全闸门。
/// </para>
/// <para>
/// 目标不是 <see cref="BusinessObject"/>（自定义 <see cref="ICommandObject"/> 实现）时直接放行：它们不持有 <see cref="Rules"/>，无从运行规则。
/// </para>
/// </remarks>
internal static class ObjectRuleGuard
{
	/// <summary>
	/// 运行目标的完整规则校验（先属性级、再对象级）；存在 Error 级违规时抛出<see cref="ValidationException"/>。
	/// </summary>
	/// <param name="target">目标业务对象。</param>
	/// <param name="message">验证异常的说明。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// 规则检查被挂起时不给出结论、也不抛出（见 <see cref="Rules.IsRuleCheckingSuspended"/>）。
	/// </remarks>
	internal static Task EnsureRulesAsync(object target, string message, CancellationToken cancellationToken = default)
	{
		return target is BusinessObject businessObject
			       ? businessObject.EnsureRulesAsync(message, cancellationToken)
			       : Task.CompletedTask;
	}
}
