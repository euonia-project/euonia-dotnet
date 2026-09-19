using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 策略树中的一个叶子条件，用于审计（解释「为什么可访问 / 为什么被拒绝」）。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <param name="Description">条件的可读描述，例如 <c>Grant(dept)</c>。</param>
/// <param name="Condition">条件的判定表达式。</param>
/// <param name="IsDeny">指示该条件在最终语义中是允许条件还是拒绝条件。</param>
internal sealed record ScopePolicyLeaf<T>(string Description, Expression<Func<T, bool>> Condition, bool IsDeny)
	where T : class;
