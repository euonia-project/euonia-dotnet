using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 恒真规约。
/// </summary>
/// <typeparam name="TEntity">此规约中的实体类型。</typeparam>
public sealed class TrueSpecification<TEntity>
    : Specification<TEntity>
    where TEntity : class
{
    #region Specification overrides

    /// <inheritdoc />
    /// <returns>恒为 true 的谓词表达式。</returns>
    public override Expression<Func<TEntity, bool>> Satisfy()
    {
        // 创建 "result" 变量将临时执行计划转换为预编译计划
        // 更多信息：http://geeks.ms/blogs/unai/2010/07/91/ef-4-0-performance-tips-1.aspx
        var result = true;

        Expression<Func<TEntity, bool>> trueExpression = t => result;
        return trueExpression;
    }

    #endregion
}
