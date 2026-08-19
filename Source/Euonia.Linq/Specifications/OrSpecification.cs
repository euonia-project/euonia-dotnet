using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 逻辑 OR 规约。
/// </summary>
/// <typeparam name="T">检查此规约的实体类型。</typeparam>
public sealed class OrSpecification<T> : Specification<T>
    where T : class
{
    #region Members

    private readonly ISpecification<T> _right;
    private readonly ISpecification<T> _left;

    #endregion

    #region Public Constructor

    /// <summary>
    /// OrSpecification 的默认构造函数。
    /// </summary>
    /// <param name="leftSide">左侧规约。</param>
    /// <param name="rightSide">右侧规约。</param>
    /// <exception cref="ArgumentNullException"><paramref name="leftSide"/> 或 <paramref name="rightSide"/> 为 <see langword="null"/>。</exception>
    public OrSpecification(ISpecification<T> leftSide, ISpecification<T> rightSide)
    {
        _left = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
        _right = rightSide ?? throw new ArgumentNullException(nameof(rightSide));
    }

    #endregion

    #region Composite Specification overrides

    /// <summary>
    /// 此复合元素的左侧规约。
    /// </summary>
    public ISpecification<T> Left => _left;

    /// <summary>
    /// 此复合元素的右侧规约。
    /// </summary>
    public ISpecification<T> Right => _right;

    /// <inheritdoc />
    /// <returns>左右规约取逻辑或后的谓词表达式。</returns>
    public override Expression<Func<T, bool>> Satisfy()
    {
        var left = _left.Satisfy();
        var right = _right.Satisfy();

        return (left.Or(right));

    }

    #endregion
}
