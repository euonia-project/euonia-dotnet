using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 把表达式中的参数替换为另一个参数实例。
/// </summary>
/// <remarks>
/// 数据权限的每个叶子条件都来自不同的表达式（模型里的维度选择器、策略里的谓词），
/// 各自持有自己的参数实例。要组合成一棵表达式树，必须先把它们统一到同一个参数上。
/// </remarks>
internal sealed class ScopeParameterReplacer : ExpressionVisitor
{
	private readonly ParameterExpression _source;
	private readonly ParameterExpression _target;

	private ScopeParameterReplacer(ParameterExpression source, ParameterExpression target)
	{
		_source = source;
		_target = target;
	}

	/// <summary>
	/// 把 <paramref name="lambda"/> 的参数替换为 <paramref name="target"/>，返回其 body。
	/// </summary>
	/// <param name="lambda">源表达式。</param>
	/// <param name="target">目标参数。</param>
	/// <returns>替换后的 body。</returns>
	internal static Expression Replace(LambdaExpression lambda, ParameterExpression target)
	{
		var replacer = new ScopeParameterReplacer(lambda.Parameters[0], target);
		return replacer.Visit(lambda.Body);
	}

	/// <inheritdoc />
	protected override Expression VisitParameter(ParameterExpression node)
	{
		return node == _source ? _target : base.VisitParameter(node);
	}
}
