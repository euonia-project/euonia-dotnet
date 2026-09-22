using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 策略归约结果：一对允许/拒绝表达式（body 层，共用规范参数）以及是否包含允许条件。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// 判定语义恒为 <c>Allow &amp;&amp; !Deny</c>。
/// </remarks>
internal sealed class ScopePolicyNode<T>
	where T : class
{
	/// <summary>
	/// 表示恒真。
	/// </summary>
	internal static readonly Expression True = Expression.Constant(true, typeof(bool));

	/// <summary>
	/// 表示恒假。
	/// </summary>
	internal static readonly Expression False = Expression.Constant(false, typeof(bool));

	internal ScopePolicyNode(Expression allow, Expression deny, bool hasAllow)
	{
		Allow = allow ?? False;
		Deny = deny ?? False;
		HasAllow = hasAllow;
	}

	/// <summary>
	/// 获取允许条件。
	/// </summary>
	internal Expression Allow { get; }

	/// <summary>
	/// 获取拒绝条件。
	/// </summary>
	internal Expression Deny { get; }

	/// <summary>
	/// 获取本节点是否提供了允许条件。
	/// </summary>
	/// <remarks>
	/// <see cref="ScopePolicy{T}.Any"/> 归约时必须据此把「只带拒绝条件」的分支排除在「或」之外，
	/// 否则 <c>Any(Grant("dept"), Deny(...))</c> 会退化成恒真——这是一个静默提权。
	/// </remarks>
	internal bool HasAllow { get; }

	/// <summary>
	/// 创建一个只有允许条件的节点。
	/// </summary>
	internal static ScopePolicyNode<T> AllowOnly(Expression allow)
	{
		return new ScopePolicyNode<T>(allow, False, true);
	}

	/// <summary>
	/// 创建一个只有拒绝条件的节点。
	/// </summary>
	internal static ScopePolicyNode<T> DenyOnly(Expression deny)
	{
		return new ScopePolicyNode<T>(False, deny, false);
	}

	/// <summary>
	/// 判断表达式是否为布尔常量 <see langword="false"/>。
	/// </summary>
	/// <param name="expression">待判断的表达式。</param>
	/// <returns>是布尔常量 false 则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>用于在归约时剔除恒假的拒绝条件，避免生成 <c>x || false</c> 这类无意义节点。</remarks>
	internal static bool IsConstantFalse(Expression expression)
	{
		return expression is ConstantExpression { Value: false };
	}

	/// <summary>
	/// 用指定的合并方式折叠多个表达式；序列为空时返回 <paramref name="empty"/>。
	/// </summary>
	internal static Expression Fold(IReadOnlyList<Expression> expressions, Func<Expression, Expression, Expression> merge, Expression empty)
	{
		if (expressions.Count == 0)
		{
			return empty;
		}

		var result = expressions[0];
		for (var index = 1; index < expressions.Count; index++)
		{
			result = merge(result, expressions[index]);
		}

		return result;
	}
}
