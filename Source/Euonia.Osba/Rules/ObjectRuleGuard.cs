namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 对象级规则在「保存」与「命令执行」两条路径上的统一强制点，
/// 与 <see cref="ObjectAuthorization"/>（操作权限）、<see cref="ScopeAuthorization"/>（数据权限）并列。
/// </summary>
/// <remarks>
/// <para>
/// 可编辑对象的保存历来会先跑对象级规则（见 <see cref="EditableObject{T}.SaveAsync(bool, CancellationToken)"/>），
/// 但命令对象的执行过去<b>完全不跑规则</b>——命令对象自己 <c>AddRules()</c> 声明的对象级规则是死代码。
/// 本类把这条路径补齐，两条路径共用 <see cref="Rules.EnsureObjectRulesAsync"/>，
/// 因此失败形态一致：<see cref="Nerosoft.Euonia.Validation.ValidationException"/> 且
/// <c>Errors</c> 携带逐条违规（属性名 + 消息）。
/// </para>
/// <para>
/// <b>本类不是安全强制点</b>：规则可被 <see cref="Rules.SuppressRuleChecking"/> 与执行器的
/// <c>WithoutRuleChecks()</c> 跳过。越权始终由工厂边界的 <see cref="ObjectAuthorization"/> /
/// <see cref="ScopeAuthorization"/> 拦截并抛 <see cref="System.Security.SecurityException"/>。
/// </para>
/// <para>
/// 目标不是 <see cref="BusinessObject"/>（自定义 <see cref="ICommandObject"/> 实现）时直接放行：
/// 它们不持有 <see cref="Rules"/>，无从运行规则。
/// </para>
/// </remarks>
internal static class ObjectRuleGuard
{
	/// <summary>
	/// 运行目标的对象级规则；存在 Error 级违规时抛出
	/// <see cref="Nerosoft.Euonia.Validation.ValidationException"/>。
	/// </summary>
	/// <param name="target">目标业务对象。</param>
	/// <param name="message">验证异常的说明。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步检查操作的任务。</returns>
	/// <remarks>
	/// 规则检查被挂起时不给出结论、也不抛出（见 <see cref="Rules.IsRuleCheckingSuspended"/>）。
	/// </remarks>
	internal static Task EnsureObjectRulesAsync(object target, string message, CancellationToken cancellationToken = default)
	{
		return target is BusinessObject businessObject
			       ? businessObject.RuleSet.EnsureObjectRulesAsync(cascade: true, message, cancellationToken)
			       : Task.CompletedTask;
	}
}
