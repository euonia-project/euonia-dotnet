using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 使用逻辑 NOT 运算符将原始规约取反。
/// </summary>
/// <typeparam name="TEntity">此规约的元素类型。</typeparam>
public sealed class NotSpecification<TEntity> : Specification<TEntity>
    where TEntity : class
{
    #region Members

    private readonly Expression<Func<TEntity, bool>> _predicate;

    #endregion

    #region Constructor

    /// <summary>
    /// NotSpecification 的构造函数。
    /// </summary>
    /// <param name="originalSpecification">原始规约。</param>
    /// <exception cref="ArgumentNullException"><paramref name="originalSpecification"/> 为 <see langword="null"/>。</exception>
    public NotSpecification(ISpecification<TEntity> originalSpecification)
    {

        if (originalSpecification == null)
        {
            throw new ArgumentNullException(nameof(originalSpecification));
        }

        _predicate = originalSpecification.Satisfy();
    }

    /// <summary>
    /// NotSpecification 的构造函数。
    /// </summary>
    /// <param name="originalSpecification">原始谓词表达式。</param>
    /// <exception cref="ArgumentNullException"><paramref name="originalSpecification"/> 为 <see langword="null"/>。</exception>
    public NotSpecification(Expression<Func<TEntity, bool>> originalSpecification)
    {
        _predicate = originalSpecification ?? throw new ArgumentNullException(nameof(originalSpecification));
    }

    #endregion

    #region Override Specification methods

    /// <inheritdoc />
    /// <returns>原始表达式取逻辑非后的谓词表达式。</returns>
    public override Expression<Func<TEntity, bool>> Satisfy()
    {
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Not(_predicate.Body),
                                                     _predicate.Parameters.Single());
    }

    #endregion
}
