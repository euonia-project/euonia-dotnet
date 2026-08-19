using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 逻辑 AND 规约。
/// </summary>
/// <typeparam name="T">检查此规约的实体类型。</typeparam>
public sealed class AndSpecification<T> : Specification<T>
   where T : class
{
    #region Members

    private readonly ISpecification<T> _rightSideSpecification;
    private readonly ISpecification<T> _leftSideSpecification;

    #endregion

    #region Public Constructor

    /// <summary>
    /// AndSpecification 的默认构造函数。
    /// </summary>
    /// <param name="leftSide">左侧规约。</param>
    /// <param name="rightSide">右侧规约。</param>
    /// <exception cref="ArgumentNullException"><paramref name="leftSide"/> 或 <paramref name="rightSide"/> 为 <see langword="null"/>。</exception>
    public AndSpecification(ISpecification<T> leftSide, ISpecification<T> rightSide)
    {
        _leftSideSpecification = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
        _rightSideSpecification = rightSide ?? throw new ArgumentNullException(nameof(rightSide));
    }

    #endregion

    #region Composite Specification overrides

    /// <summary>
    /// 此复合元素的左侧规约。
    /// </summary>
    public ISpecification<T> Left => _leftSideSpecification;

    /// <summary>
    /// 此复合元素的右侧规约。
    /// </summary>
    public ISpecification<T> Right => _rightSideSpecification;

    /// <inheritdoc />
    /// <returns>左右规约取逻辑与后的谓词表达式。</returns>
    public override Expression<Func<T, bool>> Satisfy()
    {
        var left = _leftSideSpecification.Satisfy();
        var right = _rightSideSpecification.Satisfy();

        return (left.And(right));

    }

    #endregion
}
