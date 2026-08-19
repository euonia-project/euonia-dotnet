using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示一个可用于对可查询序列进行排序的类。
/// </summary>
/// <typeparam name="T">要排序的元素类型。</typeparam>
public sealed class Orderable<T>
{
    /// <summary>
    /// 初始化 <see cref="Orderable{T}"/> 类的新实例。
    /// </summary>
    /// <param name="enumerable">要排序的查询。</param>
    public Orderable(IQueryable<T> enumerable)
    {
        Queryable = enumerable;
    }

    /// <summary>
    /// 获取或设置要排序的可查询序列。
    /// </summary>
    public IQueryable<T> Queryable { get; private set; }

    /// <summary>
    /// 按指定的键选择器对序列中的元素进行升序排序。
    /// </summary>
    /// <typeparam name="TKey">排序键的类型。</typeparam>
    /// <param name="keySelector">用于提取排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public Orderable<T> Ascending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Queryable = Queryable.OrderBy(keySelector);
        return this;
    }

    /// <summary>
    /// 按两个键选择器对序列中的元素进行升序排序。
    /// </summary>
    /// <typeparam name="TKey1">第一个排序键的类型。</typeparam>
    /// <typeparam name="TKey2">第二个排序键的类型。</typeparam>
    /// <param name="keySelector1">用于提取第一个排序键的表达式。</param>
    /// <param name="keySelector2">用于提取第二个排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public Orderable<T> Ascending<TKey1, TKey2>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2)
    {
        Queryable = Queryable.OrderBy(keySelector1)
                             .ThenBy(keySelector2);
        return this;
    }

    /// <summary>
    /// 按三个键选择器对序列中的元素进行升序排序。
    /// </summary>
    /// <typeparam name="TKey1">第一个排序键的类型。</typeparam>
    /// <typeparam name="TKey2">第二个排序键的类型。</typeparam>
    /// <typeparam name="TKey3">第三个排序键的类型。</typeparam>
    /// <param name="keySelector1">用于提取第一个排序键的表达式。</param>
    /// <param name="keySelector2">用于提取第二个排序键的表达式。</param>
    /// <param name="keySelector3">用于提取第三个排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public Orderable<T> Ascending<TKey1, TKey2, TKey3>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2, Expression<Func<T, TKey3>> keySelector3)
    {
        Queryable = Queryable.OrderBy(keySelector1)
                             .ThenBy(keySelector2)
                             .ThenBy(keySelector3);
        return this;
    }

    /// <summary>
    /// 按指定的键选择器对序列中的元素进行降序排序。
    /// </summary>
    /// <typeparam name="TKey">排序键的类型。</typeparam>
    /// <param name="keySelector">用于提取排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public Orderable<T> Descending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Queryable = Queryable.OrderByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// 按两个键选择器对序列中的元素进行降序排序。
    /// </summary>
    /// <typeparam name="TKey1">第一个排序键的类型。</typeparam>
    /// <typeparam name="TKey2">第二个排序键的类型。</typeparam>
    /// <param name="keySelector1">用于提取第一个排序键的表达式。</param>
    /// <param name="keySelector2">用于提取第二个排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public Orderable<T> Descending<TKey1, TKey2>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2)
    {
        Queryable = Queryable.OrderByDescending(keySelector1)
                             .ThenByDescending(keySelector2);
        return this;
    }

    /// <summary>
    /// 按三个键选择器对序列中的元素进行降序排序。
    /// </summary>
    /// <typeparam name="TKey1">第一个排序键的类型。</typeparam>
    /// <typeparam name="TKey2">第二个排序键的类型。</typeparam>
    /// <typeparam name="TKey3">第三个排序键的类型。</typeparam>
    /// <param name="keySelector1">用于提取第一个排序键的表达式。</param>
    /// <param name="keySelector2">用于提取第二个排序键的表达式。</param>
    /// <param name="keySelector3">用于提取第三个排序键的表达式。</param>
    /// <returns>当前实例，以便继续链式调用。</returns>
    public Orderable<T> Descending<TKey1, TKey2, TKey3>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2, Expression<Func<T, TKey3>> keySelector3)
    {
        Queryable = Queryable.OrderByDescending(keySelector1)
                             .ThenByDescending(keySelector2)
                             .ThenByDescending(keySelector3);
        return this;
    }
}
