namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 提供规约（specification）的组合扩展方法。
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// 使用逻辑 AND 运算符组合两个规约。
    /// </summary>
    /// <typeparam name="T">规约实体类型。</typeparam>
    /// <param name="first">第一个规约。</param>
    /// <param name="second">第二个规约。</param>
    /// <returns>组合后的规约。</returns>
    public static Specification<T> And<T>(this ISpecification<T> first, ISpecification<T> second)
        where T : class
    {
        var specification = (Specification<T>)first;
        specification &= (Specification<T>)second;
        return specification;
    }

    /// <summary>
    /// 当条件为 <see langword="true"/> 时，使用逻辑 AND 运算符组合两个规约。
    /// </summary>
    /// <typeparam name="T">规约实体类型。</typeparam>
    /// <param name="first">第一个规约。</param>
    /// <param name="condition">是否执行组合的条件。</param>
    /// <param name="second">用于获取第二个规约的委托。</param>
    /// <returns>组合后的规约；条件为 <see langword="false"/> 时返回 <paramref name="first"/>。</returns>
    public static Specification<T> AndIf<T>(this ISpecification<T> first, bool condition, Func<ISpecification<T>> second)
        where T : class
    {
        var specification = (Specification<T>)first;
        return !condition ? specification : specification.And(second());
    }

    /// <summary>
    /// 当条件委托返回 <see langword="true"/> 时，使用逻辑 AND 运算符组合两个规约。
    /// </summary>
    /// <typeparam name="T">规约实体类型。</typeparam>
    /// <param name="first">第一个规约。</param>
    /// <param name="selector">返回是否执行组合的条件委托。</param>
    /// <param name="second">用于获取第二个规约的委托。</param>
    /// <returns>组合后的规约；条件为 <see langword="false"/> 时返回 <paramref name="first"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> 为 <see langword="null"/>。</exception>
    public static Specification<T> AndIf<T>(this ISpecification<T> first, Func<bool> selector, Func<ISpecification<T>> second)
        where T : class
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var specification = (Specification<T>)first;
        var condition = selector();
        return !condition ? specification : specification.And(second());
    }
}
