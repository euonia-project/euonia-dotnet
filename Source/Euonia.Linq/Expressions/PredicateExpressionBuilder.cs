using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 谓词表达式生成器。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public class PredicateExpressionBuilder<TEntity>
{
    /// <summary>
    /// 初始化谓词表达式生成器。
    /// </summary>
    public PredicateExpressionBuilder()
    {
        _parameter = Expression.Parameter(typeof(TEntity), "t");
    }

    /// <summary>
    /// 参数。
    /// </summary>
    private readonly ParameterExpression _parameter;
    /// <summary>
    /// 结果表达式。
    /// </summary>
    private Expression _result;

    /// <summary>
    /// 获取参数。
    /// </summary>
    public ParameterExpression GetParameter()
    {
        return _parameter;
    }

    /// <summary>
    /// 添加表达式。
    /// </summary>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <param name="property">属性表达式。</param>
    /// <param name="operator">运算符。</param>
    /// <param name="value">值。</param>
    public void Append<TProperty>(Expression<Func<TEntity, TProperty>> property, QueryOperator @operator, object value)
    {
        _result = _result.And(_parameter.Property(Lambda.GetMember(property)).Operation(@operator, value));
    }

    /// <summary>
    /// 添加表达式。
    /// </summary>
    /// <param name="property">属性名。</param>
    /// <param name="operator">运算符。</param>
    /// <param name="value">值。</param>
    public void Append(string property, QueryOperator @operator, object value)
    {
        _result = _result.And(_parameter.Property(property).Operation(@operator, value));
    }

    /// <summary>
    /// 转换为 Lambda 表达式。
    /// </summary>
    /// <returns>生成的谓词表达式；未添加任何条件时返回恒真表达式。</returns>
    public Expression<Func<TEntity, bool>> ToLambda()
    {
        return _result == null ? PredicateBuilder.True<TEntity>() : _result.ToLambda<Func<TEntity, bool>>(_parameter);
    }
}
