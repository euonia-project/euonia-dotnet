using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 提供执行 Lambda 表达式操作的方法。
/// </summary>
public static class Lambda
{
    /// <summary>
    /// 获取表达式的成员信息。
    /// </summary>
    /// <param name="expression">Lambda 表达式。</param>
    public static MemberInfo GetMember(Expression expression)
    {
        var memberExpression = GetMemberExpression(expression);
        return memberExpression?.Member;
    }

    /// <summary>
    /// 获取成员访问表达式。
    /// </summary>
    /// <param name="expression">Lambda 表达式。</param>
    public static MemberExpression GetMemberExpression(Expression expression)
    {
        if (expression == null)
        {
            return null;
        }
        return expression.NodeType switch
        {
            ExpressionType.Lambda => GetMemberExpression(((LambdaExpression)expression).Body),
            ExpressionType.Convert => GetMemberExpression(((UnaryExpression)expression).Operand),
            ExpressionType.MemberAccess => (MemberExpression)expression,
            _ => null,
        };
    }

    /// <summary>
    /// 获取表达式的成员名称。
    /// </summary>
    /// <param name="expression">Lambda 表达式。</param>
    /// <remarks>表达式：t =&gt; t.Name == "A"，返回：Name</remarks>
    public static string GetName(Expression expression)
    {
        var memberExpression = GetMemberExpression(expression);
        return GetMemberName(memberExpression);
    }

    /// <summary>
    /// 获取 <paramref name="memberExpression"/> 的成员名称。
    /// </summary>
    /// <param name="memberExpression">成员访问表达式。</param>
    public static string GetMemberName(MemberExpression memberExpression)
    {
        if (memberExpression == null)
        {
            return string.Empty;
        }
        var result = memberExpression.ToString();
        var index = result.IndexOf(".", StringComparison.Ordinal) + 1;
        return result[index..];
    }

    /// <summary>
    /// 获取多个元素表达式的名称列表。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="expression">属性表达式，例如 t =&gt; new object[]{t.A,t.B}。</param>
    public static List<string> GetNames<T>(Expression<Func<T, object[]>> expression)
    {
        var result = new List<string>();
        if (expression == null)
        {
            return result;
        }
        if (expression.Body is not NewArrayExpression arrayExpression)
        {
            return result;
        }
        foreach (var each in arrayExpression.Expressions)
        {
            AddName(result, each);
        }
        return result;
    }

    /// <summary>
    /// 将表达式名称添加到列表。
    /// </summary>
    private static void AddName(List<string> result, Expression expression)
    {
        var name = GetName(expression);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        result.Add(name);
    }

    /// <summary>
    /// 获取表达式参数的值。
    /// </summary>
    /// <param name="expression">Lambda 表达式。</param>
    /// <example>表达式：t =&gt; t.Name == "A"，返回："A"</example>
    public static object GetValue(Expression expression)
    {
        if (expression == null)
        {
            return null;
        }
#pragma warning disable IDE0066 
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                return GetValue(((LambdaExpression)expression).Body);
            case ExpressionType.Convert:
                return GetValue(((UnaryExpression)expression).Operand);
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThanOrEqual:
                return GetValue(((BinaryExpression)expression).Right);
            case ExpressionType.Call:
                return GetMethodCallExpressionValue(expression);
            case ExpressionType.MemberAccess:
                return GetMemberValue((MemberExpression)expression);
            case ExpressionType.Constant:
                return GetConstantExpressionValue(expression);
        }
#pragma warning restore IDE0066

        return null;
    }

    /// <summary>
    /// 获取方法调用表达式的值。
    /// </summary>
    private static object GetMethodCallExpressionValue(Expression expression)
    {
        var methodCallExpression = (MethodCallExpression)expression;
        var value = GetValue(methodCallExpression.Arguments.FirstOrDefault());
        if (value != null)
        {
            return value;
        }
        return GetValue(methodCallExpression.Object);
    }

    /// <summary>
    /// 获取属性表达式的值。
    /// </summary>
    private static object GetMemberValue(MemberExpression expression)
    {
        if (expression == null)
            return null;
        var field = expression.Member as FieldInfo;
        if (field != null)
        {
            var constValue = GetConstantExpressionValue(expression.Expression);
            return field.GetValue(constValue);
        }

        var property = expression.Member as PropertyInfo;
        if (property == null)
            return null;
        if (expression.Expression == null)
            return property.GetValue(null);
        var value = GetMemberValue(expression.Expression as MemberExpression);
        if (value == null)
            return null;
        return property.GetValue(value);
    }

    /// <summary>
    /// 获取常量表达式的值。
    /// </summary>
    private static object GetConstantExpressionValue(Expression expression)
    {
        var constantExpression = (ConstantExpression)expression;
        return constantExpression.Value;
    }

    /// <summary>
    /// 获取参数，范例：t.Name，返回 t。
    /// </summary>
    /// <param name="expression">表达式，范例：t.Name。</param>
    public static ParameterExpression GetParameter(Expression expression)
    {
        if (expression == null)
        {
            return null;
        }
#pragma warning disable IDE0066 // 将 switch 语句转换为表达式
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                return GetParameter(((LambdaExpression)expression).Body);
            case ExpressionType.Convert:
                return GetParameter(((UnaryExpression)expression).Operand);
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThanOrEqual:
                return GetParameter(((BinaryExpression)expression).Left);
            case ExpressionType.MemberAccess:
                return GetParameter(((MemberExpression)expression).Expression);
            case ExpressionType.Call:
                return GetParameter(((MethodCallExpression)expression).Object);
            case ExpressionType.Parameter:
                return (ParameterExpression)expression;
        }
#pragma warning restore IDE0066 // 将 switch 语句转换为表达式

        return null;
    }

    /// <summary>
    /// 获取查询条件个数。
    /// </summary>
    /// <param name="expression">谓词表达式，范例1：t =&gt; t.Name == "A"，结果1。
    /// 范例2：t =&gt; t.Name == "A" &amp;&amp; t.Age =1，结果2。</param>
    public static int GetConditionCount(LambdaExpression expression)
    {
        if (expression == null)
            return 0;
        var result = expression.ToString().Replace("AndAlso", "|").Replace("OrElse", "|");
        return result.Split('|').Length;
    }

    /// <summary>
    /// 获取特性。
    /// </summary>
    /// <typeparam name="TAttribute">特性类型。</typeparam>
    /// <param name="expression">属性表达式。</param>
    public static TAttribute GetAttribute<TAttribute>(Expression expression) where TAttribute : Attribute
    {
        var memberInfo = GetMember(expression);
        return memberInfo.GetCustomAttribute<TAttribute>();
    }

    /// <summary>
    /// 获取特性。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <typeparam name="TAttribute">特性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式。</param>
    public static TAttribute GetAttribute<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression) where TAttribute : Attribute
    {
        return GetAttribute<TAttribute>(propertyExpression);
    }

    /// <summary>
    /// 获取特性。
    /// </summary>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <typeparam name="TAttribute">特性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式。</param>
    public static TAttribute GetAttribute<TProperty, TAttribute>(Expression<Func<TProperty>> propertyExpression) where TAttribute : Attribute
    {
        return GetAttribute<TAttribute>(propertyExpression);
    }

    /// <summary>
    /// 获取特性列表。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <typeparam name="TAttribute">特性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式。</param>
    public static IEnumerable<TAttribute> GetAttributes<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression) where TAttribute : Attribute
    {
        var memberInfo = GetMember(propertyExpression);
        return memberInfo.GetCustomAttributes<TAttribute>();
    }

    /// <summary>
    /// 获取常量表达式。
    /// </summary>
    /// <param name="expression">表达式。</param>
    /// <param name="value">值。</param>
    public static ConstantExpression Constant(Expression expression, object value)
    {
        if (expression is not MemberExpression memberExpression)
        {
            return Expression.Constant(value);
        }
        return Expression.Constant(value, memberExpression.Type);
    }

    /// <summary>
    /// 创建等于运算 Lambda 表达式。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> Equal<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .Equal(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 创建参数。
    /// </summary>
    private static ParameterExpression CreateParameter<T>()
    {
        return Expression.Parameter(typeof(T), "t");
    }

    /// <summary>
    /// 创建不等于运算 Lambda 表达式。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> NotEqual<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .NotEqual(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 创建大于运算 Lambda 表达式。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> Greater<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .Greater(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 创建大于等于运算 Lambda 表达式。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> GreaterEqual<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .GreaterEqual(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 创建小于运算 Lambda 表达式。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> Less<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .Less(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 创建小于等于运算 Lambda 表达式。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> LessEqual<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .LessEqual(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 调用 StartsWith 方法。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> Starts<T>(string propertyName, string value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .StartsWith(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 调用 EndsWith 方法。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> Ends<T>(string propertyName, string value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .EndsWith(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 调用 Contains 方法。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    public static Expression<Func<T, bool>> Contains<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter.Property(propertyName)
                        .Contains(value)
                        .ToLambda<Func<T, bool>>(parameter);
    }

    /// <summary>
    /// 解析为谓词表达式。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="propertyName">属性名。</param>
    /// <param name="value">值。</param>
    /// <param name="operator">运算符。</param>
    public static Expression<Func<T, bool>> ParsePredicate<T>(string propertyName, object value, QueryOperator @operator)
    {
        var parameter = Expression.Parameter(typeof(T), "t");
        return parameter.Property(propertyName).Operation(@operator, value).ToLambda<Func<T, bool>>(parameter);
    }
}
