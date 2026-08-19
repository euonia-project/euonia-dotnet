using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示一个用于替换表达式中参数的访问器。
/// </summary>
internal class ParameterRebinder : ExpressionVisitor
{
    /// <summary>
    /// <see cref="ParameterExpression"/> 映射表。
    /// </summary>
    readonly Dictionary<ParameterExpression, ParameterExpression> _map;

    /// <summary>
    /// 初始化 <see cref="ParameterRebinder"/> 类的新实例。
    /// </summary>
    /// <param name="map">参数映射表。</param>
    private ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
    {
        _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
    }

    /// <summary>
    /// 替换表达式中的参数。
    /// </summary>
    /// <param name="map">参数映射表。</param>
    /// <param name="exp">表达式。</param>
    /// <returns>替换后的表达式。</returns>
    public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
    {
        return new ParameterRebinder(map).Visit(exp);
    }

    /// <summary>
    /// 访问参数节点。
    /// </summary>
    /// <param name="node">参数节点。</param>
    /// <returns>替换后的表达式。</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {

        if (_map.TryGetValue(node, out ParameterExpression replacement))
        {
            node = replacement;
        }

        return base.VisitParameter(node);
    }
}
