using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 直接规约是规约的一种简单实现，在构造函数中通过 lambda 表达式获得。
/// </summary>
/// <typeparam name="TEntity">检查此规约的实体类型。</typeparam>
public sealed class DirectSpecification<TEntity> : Specification<TEntity>
    where TEntity : class
{
    #region Members

    private readonly Expression<Func<TEntity, bool>> _predicate;

    #endregion

    #region Constructor

    /// <summary>
    /// DirectSpecification 的默认构造函数。
    /// </summary>
    /// <param name="predicate">匹配的查询条件。</param>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 <see langword="null"/>。</exception>
    public DirectSpecification(Expression<Func<TEntity, bool>> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    #endregion

    #region Override

    /// <inheritdoc />
    /// <returns>构造函数中传入的谓词表达式。</returns>
    public override Expression<Func<TEntity, bool>> Satisfy()
    {
        return _predicate;
    }

    #endregion
}