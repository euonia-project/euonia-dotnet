using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示一个表达式规约（Specification）。
/// </summary>
/// <remarks>
/// 规约重载运算符以创建 AND、OR 或 NOT 规约。
/// 此外，以二元 And 与二元 Or 的相同语义重载了 AND 和 OR 运算符。
/// C# 无法直接重载 AND 与 OR 运算符，因为框架不允许这样做。但通过重载 false 与 true 运算符可以实现该行为。
/// 相关说明请阅读 http://msdn.microsoft.com/en-us/library/aa691312(VS.71).aspx
/// </remarks>
/// <typeparam name="TEntity">条件中的项类型。</typeparam>
public abstract class Specification<TEntity> : ISpecification<TEntity>
     where TEntity : class
{
    #region ISpecification<TEntity> Members

    /// <summary>
    /// 规约模式的 IsSatisfied 方法。
    /// </summary>
    /// <returns>满足此规约的表达式。</returns>
    public abstract Expression<Func<TEntity, bool>> Satisfy();

    #endregion

    #region Override Operators

    /// <summary>
    /// AND 运算符。
    /// </summary>
    /// <param name="leftSideSpecification">AND 运算的左操作数。</param>
    /// <param name="rightSideSpecification">AND 运算的右操作数。</param>
    /// <returns>新的规约。</returns>
    public static Specification<TEntity> operator &(Specification<TEntity> leftSideSpecification, Specification<TEntity> rightSideSpecification)
    {
        return new AndSpecification<TEntity>(leftSideSpecification, rightSideSpecification);
    }

    /// <summary>
    /// OR 运算符。
    /// </summary>
    /// <param name="leftSideSpecification">OR 运算的左操作数。</param>
    /// <param name="rightSideSpecification">OR 运算的右操作数。</param>
    /// <returns>新的规约。</returns>
    public static Specification<TEntity> operator |(Specification<TEntity> leftSideSpecification, Specification<TEntity> rightSideSpecification)
    {
        return new OrSpecification<TEntity>(leftSideSpecification, rightSideSpecification);
    }

    /// <summary>
    /// NOT 运算符。
    /// </summary>
    /// <param name="specification">要取反的规约。</param>
    /// <returns>新的规约。</returns>
    public static Specification<TEntity> operator !(Specification<TEntity> specification)
    {
        return new NotSpecification<TEntity>(specification);
    }

    /// <summary>
    /// 重载 false 运算符，仅用于支持 AND、OR 运算符。
    /// </summary>
    /// <param name="specification">规约实例。</param>
    /// <returns>参见 C# 中的 false 运算符。</returns>
    public static bool operator false(Specification<TEntity> specification)
    {
        return false;
    }

    /// <summary>
    /// 重载 true 运算符，仅用于支持 AND、OR 运算符。
    /// </summary>
    /// <param name="specification">规约实例。</param>
    /// <returns>参见 C# 中的 true 运算符。</returns>
    public static bool operator true(Specification<TEntity> specification)
    {
        return false;
    }

    #endregion
}
