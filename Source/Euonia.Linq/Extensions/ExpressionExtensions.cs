using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 为 <see cref="Expression"/> 提供扩展方法。
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// 当 <paramref name="condition"/> 为 true 时，向表达式列表添加指定的谓词表达式。
    /// </summary>
    /// <remarks>
    /// 该方法就地修改 <paramref name="expressions"/> 列表，并返回同一实例。
    /// </remarks>
    /// <param name="expressions">谓词表达式列表。</param>
    /// <param name="condition">是否添加的条件。</param>
    /// <param name="expression">要添加的谓词表达式。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>添加后的表达式列表。</returns>
    public static IList<Expression<Func<T, bool>>> AddIf<T>(this IList<Expression<Func<T, bool>>> expressions, bool condition, Expression<Func<T, bool>> expression)
    {
        if (condition)
        {
            expressions.Add(expression);
        }

        return expressions;
    }

    /// <summary>
    /// 当条件委托返回 true 时，向表达式列表添加指定的谓词表达式。
    /// </summary>
    /// <remarks>
    /// 该方法就地修改 <paramref name="expressions"/> 列表，并返回同一实例。
    /// </remarks>
    /// <param name="expressions">谓词表达式列表。</param>
    /// <param name="condition">返回是否添加的条件委托。</param>
    /// <param name="expression">要添加的谓词表达式。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>添加后的表达式列表。</returns>
    public static IList<Expression<Func<T, bool>>> AddIf<T>(this IList<Expression<Func<T, bool>>> expressions, Func<bool> condition, Expression<Func<T, bool>> expression)
    {
        return expressions.AddIf(condition(), expression);
    }

    /// <summary>
    /// 使用指定的运算符将所有表达式组合为一个新表达式。
    /// </summary>
    /// <remarks>
    /// 种子表达式必须与组合运算符匹配：AndAlso 使用恒真（true）作为恒等元素，
    /// OrElse 使用恒假（false）作为恒等元素；否则 OrElse 组合会退化为恒真表达式。
    /// 当 <paramref name="expressions"/> 为空时，返回对应的种子表达式。
    /// </remarks>
    /// <param name="expressions">待组合的表达式序列。</param>
    /// <param name="type">组合运算符，默认为 <see cref="PredicateOperator.AndAlso"/>。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>组合后的谓词表达式。</returns>
    public static Expression<Func<T, bool>> Compose<T>(this IEnumerable<Expression<Func<T, bool>>> expressions, PredicateOperator type = PredicateOperator.AndAlso)
    {
        var seed = type == PredicateOperator.OrElse ? PredicateBuilder.False<T>() : PredicateBuilder.True<T>();
        return expressions.Compose(seed, type);
    }

    /// <summary>
    /// 使用指定的运算符与种子表达式将所有表达式组合为一个新表达式。
    /// </summary>
    /// <param name="expressions">待组合的表达式序列。</param>
    /// <param name="seed">种子表达式，作为组合的初始结果。</param>
    /// <param name="type">组合运算符，默认为 <see cref="PredicateOperator.AndAlso"/>。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>组合后的谓词表达式。</returns>
    public static Expression<Func<T, bool>> Compose<T>(this IEnumerable<Expression<Func<T, bool>>> expressions, Expression<Func<T, bool>> seed, PredicateOperator type = PredicateOperator.AndAlso)
    {
        var predicate = expressions.Aggregate(seed, (current, next) => Compose(current, next, type));
        return predicate;
    }

    /// <summary>
    /// 使用指定的组合运算符合并两个谓词。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <param name="type">组合运算符。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>合并后的谓词表达式。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="type"/> 不是 <see cref="PredicateOperator.AndAlso"/> 或 <see cref="PredicateOperator.OrElse"/> 时抛出。</exception>
    private static Expression<Func<T, bool>> Compose<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, PredicateOperator type)
    {
        return type switch
        {
            PredicateOperator.AndAlso => left.And(right),
            PredicateOperator.OrElse => left.Or(right),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    #region Property

    /// <summary>
    /// 为指定属性创建 <see cref="MemberExpression"/>。
    /// </summary>
    /// <param name="expression">源表达式。</param>
    /// <param name="propertyName">属性名，支持点号分隔的多级属性，例如：Name、Customer.Name。</param>
    /// <returns>表示属性访问的表达式。</returns>
    public static Expression Property(this Expression expression, string propertyName)
    {
        if (propertyName.All(t => t != '.'))
            return Expression.Property(expression, propertyName);
        var propertyNameList = propertyName.Split('.');
        Expression result = null;
        for (var i = 0; i < propertyNameList.Length; i++)
        {
            if (i == 0)
            {
                result = Expression.Property(expression, propertyNameList[0]);
                continue;
            }

            result = result.Property(propertyNameList[i]);
        }

        return result;
    }

    /// <summary>
    /// 为指定成员创建 <see cref="MemberExpression"/>。
    /// </summary>
    /// <param name="expression">源表达式。</param>
    /// <param name="member">属性成员。</param>
    /// <returns>表示成员访问的表达式。</returns>
    public static Expression Property(this Expression expression, MemberInfo member)
    {
        return Expression.MakeMemberAccess(expression, member);
    }

    #endregion

    #region And expression

    /// <summary>
    /// 使用逻辑“与”组合两个谓词。
    /// </summary>
    /// <param name="first">第一个谓词。</param>
    /// <param name="second">第二个谓词。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>组合后的谓词表达式。</returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        return first.Compose(second, Expression.AndAlso);
    }

    ///*
    /// <summary>
    /// 使用逻辑“与”（AndAlso）合并两个表达式；若其中一个为 null，则直接返回另一个。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>合并后的表达式。</returns>
    public static Expression And(this Expression left, Expression right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return Expression.AndAlso(left, right);
    }

    #endregion

    #region Or expression

    /// <summary>
    /// 使用逻辑“或”组合两个谓词。
    /// </summary>
    /// <param name="first">第一个谓词。</param>
    /// <param name="second">第二个谓词。</param>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <returns>组合后的谓词表达式；若 <paramref name="first"/> 为 null 则返回 <paramref name="second"/>，若 <paramref name="second"/> 为 null 则返回 <paramref name="first"/>。</returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        if (first == null)
        {
            return second;
        }
        if (second == null)
        {
            return first;
        }
        return first.Compose(second, Expression.OrElse);
    }

    /// <summary>
    /// 使用逻辑“或”（OrElse）合并两个表达式。
    /// </summary>
    /// <param name="first">左操作数。</param>
    /// <param name="second">右操作数。</param>
    /// <returns>合并后的表达式。</returns>
    public static Expression Or(this Expression first, Expression second)
    {
        return Expression.OrElse(first, second);
    }

    //*/
    #endregion

    #region Value

    /// <summary>
    /// 从 lambda 表达式中获取值。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="expression">lambda 表达式。</param>
    /// <returns>从表达式中提取的值。</returns>
    public static object Value<T>(this Expression<Func<T, bool>> expression)
    {
        return Lambda.GetValue(expression);
    }

    #endregion

    #region Equal

    /// <summary>
    /// 创建“等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>表示 <c>left == right</c> 的表达式。</returns>
    public static Expression Equal(this Expression left, Expression right)
    {
        return Expression.Equal(left, right);
    }

    /// <summary>
    /// 创建“等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="value">要比较的值。</param>
    /// <returns>表示 <c>left == value</c> 的表达式。</returns>
    public static Expression Equal(this Expression left, object value)
    {
        return left.Equal(Lambda.Constant(left, value));
    }

    #endregion

    #region NotEqual

    /// <summary>
    /// 创建“不等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>表示 <c>left != right</c> 的表达式。</returns>
    public static Expression NotEqual(this Expression left, Expression right)
    {
        return Expression.NotEqual(left, right);
    }

    /// <summary>
    /// 创建“不等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="value">要比较的值。</param>
    /// <returns>表示 <c>left != value</c> 的表达式。</returns>
    public static Expression NotEqual(this Expression left, object value)
    {
        return left.NotEqual(Lambda.Constant(left, value));
    }

    #endregion

    #region Greater

    /// <summary>
    /// 创建“大于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>表示 <c>left &gt; right</c> 的表达式。</returns>
    public static Expression Greater(this Expression left, Expression right)
    {
        return Expression.GreaterThan(left, right);
    }

    /// <summary>
    /// 创建“大于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="value">要比较的值。</param>
    /// <returns>表示 <c>left &gt; value</c> 的表达式。</returns>
    public static Expression Greater(this Expression left, object value)
    {
        return left.Greater(Lambda.Constant(left, value));
    }

    #endregion

    #region GreaterEqual

    /// <summary>
    /// 创建“大于或等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>表示 <c>left &gt;= right</c> 的表达式。</returns>
    public static Expression GreaterEqual(this Expression left, Expression right)
    {
        return Expression.GreaterThanOrEqual(left, right);
    }

    /// <summary>
    /// 创建“大于或等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="value">要比较的值。</param>
    /// <returns>表示 <c>left &gt;= value</c> 的表达式。</returns>
    public static Expression GreaterEqual(this Expression left, object value)
    {
        return left.GreaterEqual(Lambda.Constant(left, value));
    }

    #endregion

    #region Less

    /// <summary>
    /// 创建“小于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>表示 <c>left &lt; right</c> 的表达式。</returns>
    public static Expression Less(this Expression left, Expression right)
    {
        return Expression.LessThan(left, right);
    }

    /// <summary>
    /// 创建“小于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="value">要比较的值。</param>
    /// <returns>表示 <c>left &lt; value</c> 的表达式。</returns>
    public static Expression Less(this Expression left, object value)
    {
        return left.Less(Lambda.Constant(left, value));
    }

    #endregion

    #region LessEqual

    /// <summary>
    /// 创建“小于或等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>表示 <c>left &lt;= right</c> 的表达式。</returns>
    public static Expression LessEqual(this Expression left, Expression right)
    {
        return Expression.LessThanOrEqual(left, right);
    }

    /// <summary>
    /// 创建“小于或等于”比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="value">要比较的值。</param>
    /// <returns>表示 <c>left &lt;= value</c> 的表达式。</returns>
    public static Expression LessEqual(this Expression left, object value)
    {
        return left.LessEqual(Lambda.Constant(left, value));
    }

    #endregion

    #region StartsWith

    /// <summary>
    /// 创建“以指定值开头”的表达式。
    /// </summary>
    /// <param name="left">左操作数，通常为字符串属性表达式。</param>
    /// <param name="value">前缀值。</param>
    /// <returns>表示调用 <c>StartsWith</c> 方法的表达式。</returns>
    public static Expression StartsWith(this Expression left, object value)
    {
        return left.Call("StartsWith", new[] { typeof(string) }, value);
    }

    #endregion

    #region EndsWith

    /// <summary>
    /// 创建“以指定值结尾”的表达式。
    /// </summary>
    /// <param name="left">左操作数，通常为字符串属性表达式。</param>
    /// <param name="value">后缀值。</param>
    /// <returns>表示调用 <c>EndsWith</c> 方法的表达式。</returns>
    public static Expression EndsWith(this Expression left, object value)
    {
        return left.Call("EndsWith", new[] { typeof(string) }, value);
    }

    #endregion

    #region Contains

    /// <summary>
    /// 创建“包含指定值”的表达式。
    /// </summary>
    /// <param name="left">左操作数，通常为字符串属性表达式。</param>
    /// <param name="value">要判断包含的值。</param>
    /// <returns>表示调用 <c>Contains</c> 方法的表达式。</returns>
    public static Expression Contains(this Expression left, object value)
    {
        return left.Call("Contains", new[] { typeof(string) }, value);
    }

    #endregion

    #region Operation

    /// <summary>
    /// 根据指定的查询运算符，将左操作数与值组合成比较表达式。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="operator">查询运算符。</param>
    /// <param name="value">比较值。</param>
    /// <returns>组合后的比较表达式。</returns>
    /// <exception cref="NotImplementedException">当 <paramref name="operator"/> 尚未实现（如 NotContains、Like、NotLike、Is）时抛出。</exception>
    public static Expression Operation(this Expression left, QueryOperator @operator, object value)
    {
        return @operator switch
        {
            QueryOperator.Equal => left.Equal(value),
            QueryOperator.NotEqual => left.NotEqual(value),
            QueryOperator.GreaterThan => left.Greater(value),
            QueryOperator.GreaterThanOrEqual => left.GreaterEqual(value),
            QueryOperator.LessThan => left.Less(value),
            QueryOperator.LessThanOrEqual => left.LessEqual(value),
            QueryOperator.StartsWith => left.StartsWith(value),
            QueryOperator.EndsWith => left.EndsWith(value),
            QueryOperator.Contains => left.Contains(value),
            QueryOperator.NotContains => throw new NotImplementedException(),
            QueryOperator.Like => throw new NotImplementedException(),
            QueryOperator.NotLike => throw new NotImplementedException(),
            QueryOperator.Is => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };
    }

    #endregion

    #region Call

    /// <summary>
    /// 创建对实例方法的调用表达式。
    /// </summary>
    /// <param name="instance">实例表达式。</param>
    /// <param name="methodName">方法名。</param>
    /// <param name="values">方法参数表达式。</param>
    /// <returns>表示方法调用的表达式。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="NullReferenceException">当 <paramref name="instance"/> 的类型上不存在名为 <paramref name="methodName"/> 的方法时抛出。</exception>
    public static Expression Call(this Expression instance, string methodName, params Expression[] values)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var method = instance.Type.GetTypeInfo().GetMethod(methodName);

        if (method == null)
        {
            throw new NullReferenceException($"Method {methodName} not found.");
        }

        return Expression.Call(instance, method, values);
    }

    /// <summary>
    /// 创建对实例方法的调用表达式。
    /// </summary>
    /// <param name="instance">实例表达式。</param>
    /// <param name="methodName">方法名。</param>
    /// <param name="values">方法参数值，将转换为常量表达式。</param>
    /// <returns>表示方法调用的表达式。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="NullReferenceException">当 <paramref name="instance"/> 的类型上不存在名为 <paramref name="methodName"/> 的方法时抛出。</exception>
    public static Expression Call(this Expression instance, string methodName, params object[] values)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var method = instance.Type.GetTypeInfo().GetMethod(methodName);

        if (method == null)
        {
            throw new NullReferenceException($"Method {methodName} not found.");
        }

        if (values == null || values.Length == 0)
        {
            return Expression.Call(instance, method);
        }

        return Expression.Call(instance, method, values.Select(Expression.Constant));
    }

    /// <summary>
    /// 创建对指定参数签名的实例方法的调用表达式。
    /// </summary>
    /// <param name="instance">实例表达式。</param>
    /// <param name="methodName">方法名。</param>
    /// <param name="paramTypes">方法参数类型。</param>
    /// <param name="values">方法参数值，将转换为常量表达式。</param>
    /// <returns>表示方法调用的表达式。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="NullReferenceException">当 <paramref name="instance"/> 的类型上不存在与 <paramref name="paramTypes"/> 匹配的方法时抛出。</exception>
    public static Expression Call(this Expression instance, string methodName, Type[] paramTypes, params object[] values)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var method = instance.Type.GetTypeInfo().GetMethod(methodName, paramTypes);

        if (method == null)
        {
            throw new NullReferenceException($"Method {methodName} not found.");
        }

        if (values == null || values.Length == 0)
        {
            return Expression.Call(instance, method);
        }

        return Expression.Call(instance, method, values.Select(Expression.Constant));
    }

    #endregion

    #region Compose

    /// <summary>
    /// 使用指定的合并函数将第一个表达式与第二个表达式组合。
    /// </summary>
    /// <remarks>
    /// 第二个表达式中的参数会按位置替换为第一个表达式的参数，以保证合并后的表达式参数一致。
    /// </remarks>
    private static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
    {
        // 建立映射：将 second 的参数按位置映射到 first 的参数
        var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] })
                                  .ToDictionary(p => p.s, p => p.f);

        // 将第二个 lambda 表达式中的参数替换为第一个表达式的参数
        var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

        // 使用第一个表达式的参数创建合并后的 lambda 表达式
        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }

    #endregion

    #region ToLambda

    /// <summary>
    /// 根据指定的表达式体和参数创建 lambda 表达式。
    /// </summary>
    /// <typeparam name="TDelegate">委托类型。</typeparam>
    /// <param name="body">表达式体。</param>
    /// <param name="parameters">参数列表。</param>
    /// <returns>委托类型的 lambda 表达式；若 <paramref name="body"/> 为 null 则返回 null。</returns>
    public static Expression<TDelegate> ToLambda<TDelegate>(this Expression body, params ParameterExpression[] parameters)
    {
        if (body == null)
        {
            return null;
        }
        return Expression.Lambda<TDelegate>(body, parameters);
    }

    #endregion

    /// <summary>
    /// 对谓词取反。
    /// </summary>
    /// <param name="expression">源谓词。</param>
    /// <returns>取反后的谓词表达式。</returns>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        var negated = Expression.Not(expression.Body);
        return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
    }

    /// <summary>
    /// 使用指定的运算符扩展源谓词。
    /// </summary>
    /// <typeparam name="T">谓词参数的类型。</typeparam>
    /// <param name="first">源谓词。</param>
    /// <param name="second">要组合的谓词。</param>
    /// <param name="operator">组合运算符，可为 <see cref="PredicateOperator.AndAlso"/> 或 <see cref="PredicateOperator.OrElse"/>。</param>
    /// <returns>组合后的谓词表达式。</returns>
    public static Expression<Func<T, bool>> Extend<T>([NotNull] this Expression<Func<T, bool>> first, [NotNull] Expression<Func<T, bool>> second, PredicateOperator @operator = PredicateOperator.AndAlso)
    {
        return @operator == PredicateOperator.OrElse ? first.Or(second) : first.And(second);
    }
}
