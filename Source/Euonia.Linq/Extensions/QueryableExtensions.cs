using System.Linq.Expressions;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 为 <see cref="IQueryable{T}"/> 提供扩展方法。
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// 根据规约（specification）添加查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="criteria">查询规约。</param>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="criteria"/> 为 <see langword="null"/>。</exception>
    public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> source, ISpecification<TEntity> criteria)
        where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));
        var predicate = criteria.Satisfy();
        if (predicate == null)
            return source;
        return source.Where(predicate);
    }

    /// <summary>
    /// 当 <paramref name="condition"/> 为 <see langword="true"/> 时添加查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="predicate">查询条件。</param>
    /// <param name="condition">为 <see langword="true"/> 时应用条件，为 <see langword="false"/> 时忽略。</param>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    public static IQueryable<TEntity> WhereIf<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, bool>> predicate, bool condition) where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (condition == false)
            return source;
        return source.Where(predicate);
    }

    /// <summary>
    /// 当谓词对应的值为非空时添加查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="predicate">查询谓词。</param>
    /// <remarks>
    /// 注意：仅允许一个条件属性。
    /// 例如：
    /// <code>t =&gt; t.Name == "a"</code> —— 允许
    /// <code>t =&gt; t.Name == "a" &amp;&amp; t.Mobile == "123"</code> —— 不允许
    /// <code>t =&gt; t.Name == ""</code> —— 被忽略
    /// </remarks>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">谓词包含多个条件属性时抛出。</exception>
    public static IQueryable<TEntity> WhereIfNotEmpty<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        predicate = GetWhereIfNotEmptyExpression(predicate);
        if (predicate == null)
        {
            return source;
        }
        return source.Where(predicate);
    }

    /// <summary>
    /// 添加边界查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式，例如 t =&gt; t.Age。</param>
    /// <param name="min">最小边界值。</param>
    /// <param name="max">最大边界值。</param>
    /// <param name="boundary">指示边界值是否包含在内。</param>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    public static IQueryable<TEntity> Between<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, int? min, int? max, RangeBoundary boundary = RangeBoundary.Both) where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return source.Where(new Int32SegmentSpecification<TEntity, TProperty>(propertyExpression, min, max, boundary));
    }

    /// <summary>
    /// 添加边界查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式，例如 t =&gt; t.Age。</param>
    /// <param name="min">最小边界值。</param>
    /// <param name="max">最大边界值。</param>
    /// <param name="boundary">指示边界值是否包含在内。</param>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    public static IQueryable<TEntity> Between<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, double? min, double? max, RangeBoundary boundary = RangeBoundary.Both) where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return source.Where(new DoubleSegmentSpecification<TEntity, TProperty>(propertyExpression, min, max, boundary));
    }

    /// <summary>
    /// 添加边界查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式，例如 t =&gt; t.Price。</param>
    /// <param name="min">最小边界值。</param>
    /// <param name="max">最大边界值。</param>
    /// <param name="boundary">指示边界值是否包含在内。</param>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    public static IQueryable<TEntity> Between<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, decimal? min, decimal? max, RangeBoundary boundary = RangeBoundary.Both) where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return source.Where(new DecimalSegmentSpecification<TEntity, TProperty>(propertyExpression, min, max, boundary));
    }

    /// <summary>
    /// 添加边界查询条件。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <typeparam name="TProperty">属性类型。</typeparam>
    /// <param name="propertyExpression">属性表达式，例如 t =&gt; t.Time。</param>
    /// <param name="min">最小边界值。</param>
    /// <param name="max">最大边界值。</param>
    /// <param name="includeTime">指示是否包含时间部分。</param>
    /// <param name="boundary">指示边界值是否包含在内。</param>
    /// <returns>应用了查询条件的查询。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    public static IQueryable<TEntity> Between<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, DateTime? min, DateTime? max, bool includeTime = true, RangeBoundary? boundary = null) where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (includeTime)
            return source.Where(new DateTimeSegmentSpecification<TEntity, TProperty>(propertyExpression, min, max, boundary ?? RangeBoundary.Both));
        return source.Where(new DateSegmentSpecification<TEntity, TProperty>(propertyExpression, min, max, boundary ?? RangeBoundary.Left));
    }

    /// <summary>
    /// 获取非空查询条件表达式；当谓词对应的值为空时返回 <see langword="null"/>。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="predicate">要检查的谓词表达式。</param>
    /// <returns>值非空时返回原谓词表达式，否则返回 <see langword="null"/>。</returns>
    /// <exception cref="InvalidOperationException">谓词包含多个条件属性时抛出。</exception>
    public static Expression<Func<TEntity, bool>> GetWhereIfNotEmptyExpression<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        if (predicate == null)
        {
            return null;
        }
        if (Lambda.GetConditionCount(predicate) > 1)
        {
            throw new InvalidOperationException(string.Format("Ony one predicate is allowed: {0}", predicate));
        }
        var value = predicate.Value();
        if (string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return null;
        }
        return predicate;
    }

    /// <summary>
    /// 按指定属性对序列进行升序排序。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="propertyName">用于排序的属性名。</param>
    /// <returns>排序后的查询。</returns>
    public static IQueryable<TEntity> OrderByAscending<TEntity>(this IQueryable<TEntity> source, string propertyName)
        where TEntity : class
    {
        return source.OrderBy(propertyName, SortType.Ascending);
    }

    /// <summary>
    /// 按指定属性对序列进行降序排序。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="propertyName">用于排序的属性名。</param>
    /// <returns>排序后的查询。</returns>
    public static IQueryable<TEntity> OrderByDescending<TEntity>(this IQueryable<TEntity> source, string propertyName)
        where TEntity : class
    {
        return source.OrderBy(propertyName, SortType.Descending);
    }

    /// <summary>
    /// 按指定属性和排序方式对查询进行排序。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="propertyName">用于排序的属性名。</param>
    /// <param name="sortType">排序方式。</param>
    /// <remarks>
    /// 无法根据 <paramref name="propertyName"/> 解析出属性表达式，或 <paramref name="sortType"/> 无效时，返回原查询。
    /// </remarks>
    /// <returns>排序后的查询。</returns>
    public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string propertyName, SortType sortType)
        where TEntity : class
    {
        var expression = PropertyAccessorCache<TEntity>.Get(propertyName);
        if (expression == null)
        {
            return source;
        }

        var methodName = sortType switch
        {
            SortType.Ascending => nameof(Queryable.OrderBy),
            SortType.Descending => nameof(Queryable.OrderByDescending),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(methodName))
        {
            return source;
        }

        var resultExpression = Expression.Call(typeof(Queryable), methodName, new[] { typeof(TEntity), expression.ReturnType },
            source.Expression,
            Expression.Quote(expression));
        return source.Provider.CreateQuery<TEntity>(resultExpression);
    }

    /// <summary>
    /// 按属性名与属性值相等的关系筛选查询。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="propertyName">用于筛选的属性名。</param>
    /// <param name="propertyValue">与属性比较的值。</param>
    /// <returns>筛选后的查询。</returns>
    public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> source, string propertyName, object propertyValue)
        where TEntity : class
    {
        return source.Where(propertyName, propertyValue, Expression.Equal);
    }

    /// <summary>
    /// 使用自定义值表达式按属性名与属性值筛选查询。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="source">查询源。</param>
    /// <param name="propertyName">用于筛选的属性名。</param>
    /// <param name="propertyValue">与属性比较的值。</param>
    /// <param name="valueExpression">用于构造比较表达式的委托。</param>
    /// <remarks>
    /// 属性表达式无法解析，或值无法转换为属性类型时，返回原查询。
    /// </remarks>
    /// <returns>筛选后的查询。</returns>
    public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> source, string propertyName, object propertyValue, Func<Expression, ConstantExpression, Expression> valueExpression)
        where TEntity : class
    {
        // 1. 获取成员访问表达式
        var memberExpression = PropertyAccessorCache<TEntity>.Get(propertyName);
        if (memberExpression == null)
        {
            return source;
        }

        // 2. 尝试将值转换为正确的类型
        object value;
        try
        {
            value = Convert.ChangeType(propertyValue, memberExpression.ReturnType);
        }
        catch (InvalidCastException)
        {
            return source;
        }
        catch (FormatException)
        {
            return source;
        }
        catch (OverflowException)
        {
            return source;
        }
        catch (ArgumentNullException)
        {
            return source;
        }
        catch (SystemException)
        {
            return source;
        }

        // 3. 构造表达式树
        var calculateExpression = valueExpression(memberExpression.Body, Expression.Constant(value, memberExpression.ReturnType));

        var expression = Expression.Lambda(calculateExpression, memberExpression.Parameters[0]);

        // 4. 构造新的查询
        var resultExpression = Expression.Call(
            null,
            GetMethodInfo(Queryable.Where, source, (Expression<Func<TEntity, bool>>)null),
            new[] { source.Expression, Expression.Quote(expression) });
        return source.Provider.CreateQuery<TEntity>(resultExpression);

        // ReSharper disable UnusedParameter.Local
        static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> function, T1 t1, T2 t2)
        {
            return function.Method;
        }
        // ReSharper restore UnusedParameter.Local
    }
}